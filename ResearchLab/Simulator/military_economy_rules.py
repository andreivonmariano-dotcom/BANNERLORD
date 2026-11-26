"""Selected bounded default model arithmetic. Not a native combat/economy scheduler.
All context modifiers, skill/HP effects and random inputs must be supplied.
"""
import math
from native_rules import f32,nonnegative

def tier(level,maximum=6):
    if type(level) is not int or level<0 or type(maximum) is not int or maximum<0:raise ValueError("level/tier")
    return min(maximum,max(0,math.ceil(f32(f32(level-5)/5))))
def base_power(level,hero=False,maximum=6):
    if type(hero) is not bool or type(level) is not int or level<0:raise ValueError("level/hero")
    t=level//4+1 if hero else tier(level,maximum)
    p=f32((2+t)*(10+t)*f32(.02))
    return f32(p*1.5) if hero else p
def xp_reward(level):
    if type(level) is not int or not 0<=level<=1024:raise ValueError("level")
    return (level+6)**2//3
def morale_factor(striker,struck):
    for x in (striker,struck):
        if not 0<=nonnegative(x)<=100:raise ValueError("morale")
    return f32(f32(min(f32(striker-50),0)-max(f32(struck-50),0))*f32(.005))
def base_hit(striker_power,struck_power,advantage,draw):
    for x in (striker_power,struck_power,advantage,draw):nonnegative(x)
    if struck_power<=0 or draw>1:raise ValueError("hit inputs")
    # Python pow approximation of MathF.Pow, not native float parity established.
    p=f32(math.pow(f32(striker_power/struck_power),f32(.7)))
    return int(f32(f32(.5+f32(.5*draw))*f32(f32(40*p)*advantage)))
def security_drift(security,medium=50):
    if not 0<=nonnegative(security)<=100 or not 0<=nonnegative(medium)<=100:raise ValueError("security")
    return f32(f32(-f32(security-medium))/15)
def prosperity_security(prosperity):
    return max(-5,f32(f32(-.0005)*f32(nonnegative(prosperity))))
def housing_prosperity(prosperity,is_town):
    p=nonnegative(prosperity)
    if type(is_town) is not bool:raise ValueError("town")
    if not is_town:return 0
    for limit,delta in ((250,6),(500,5),(750,4),(1000,3),(1250,2),(1500,1)):
        if p<limit:return delta
    for limit,delta in ((21000,-6),(18000,-5),(15000,-4),(12000,-3),(9000,-2),(6000,-1)):
        if p>limit:return delta
    return 0
def surplus_food_prosperity(stock,change,limit):
    nonnegative(stock);nonnegative(limit)
    if type(change) not in (int,float) or not math.isfinite(change) or type(limit) is not int:raise ValueError("food")
    return f32(max(0,int(f32(stock+change))-limit)*f32(.1))
