"""Selected default-model arithmetic from installed 1.5.7 IL.
Independent calculations; no provider overrides, campaign callbacks or RNG.
"""
import math
import struct

def f32(x):
    return struct.unpack("<f",struct.pack("<f",x))[0]

def nonnegative(x):
    if type(x) not in (int,float) or not math.isfinite(x) or x<0:raise ValueError("nonnegative finite input required")
    return x

def market_change(prosperity,gold):
    nonnegative(prosperity);nonnegative(gold)
    if type(gold) is not int:raise ValueError("integer gold required")
    # MathF.Round(Single) delegates to Math.Round(Double), midpoint-to-even.
    return round(f32(f32(f32(10000+f32(f32(prosperity)*12))-f32(gold))*.25))

def troop_wage(tier,mercenary=False):
    if type(tier) is not int or tier<0 or type(mercenary) is not bool:raise ValueError("invalid tier/occupation")
    base=(1,2,3,5,8,12,17)[tier] if tier<7 else 23
    return int(base*1.5) if mercenary else base

def learning_limit(attributes,focus):
    if not attributes or type(focus) is not int or focus<0:raise ValueError("invalid inputs")
    total=0.0
    for a in attributes:
        nonnegative(a);total=f32(total+f32(a))
    average=f32(total/len(attributes))
    return f32(max(0,f32(f32(average-1)*10))+f32(focus*30))

def calendar(fast=False):
    if type(fast) is not bool:raise ValueError("invalid mode")
    days_per_week,weeks_per_season=(3,2) if fast else (7,3)
    return {"hours_per_day":24,"days_per_week":days_per_week,"weeks_per_season":weeks_per_season,
            "days_per_season":days_per_week*weeks_per_season,"days_per_year":days_per_week*weeks_per_season*4,
            "pregnancy_days":18 if fast else 36}

AGE_THRESHOLDS=(3,6,14,18,35,55,128)
def age_stage(age):
    nonnegative(age)
    # Descriptive simulator labels, not a native HeroState enum.
    return ("baby","infant","child","teenager","adult","middle_adult","old","at_model_max_or_older")[sum(age>=a for a in AGE_THRESHOLDS)]

def pregnancy_age_eligible(age):
    nonnegative(age);return 18<=age<=45

def age_after_days(age,days,fast=False):
    nonnegative(age);nonnegative(days)
    return age+days/calendar(fast)["days_per_year"]

def relation_after_effective_delta(previous,delta):
    if type(previous) is not int or type(delta) is not int or not -100<=previous<=100:raise ValueError("invalid relation")
    # Effective delta and actor pair must already be resolved by native/provider model.
    return max(-100,min(100,previous+delta))
