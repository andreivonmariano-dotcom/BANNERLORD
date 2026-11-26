"""Static default delegation arithmetic from installed v1.5.7.125802.
Not a native issue invocation; caller supplies resolved reference, actual skill,
base duration/men, total required men and verified scale flags.
"""
from math import ceil
from native_rules import f32
def validate(skill,reference,n):
    if any(type(x) is not int or x<0 for x in (skill,reference,n)) or n<1:raise ValueError("positive base quantity and nonnegative skills required")
def casualties(skill,reference,required):
    validate(skill,reference,required)
    fraction=f32(.8) if skill==0 else f32((reference//skill)*f32(.1))
    fraction=max(f32(.2),min(f32(.8),fraction))
    upper=ceil(f32(f32(required)*fraction))
    lower=max(1,min(upper,2*(upper//3)))
    return lower,upper
def duration(skill,reference,base):
    validate(skill,reference,base)
    return base+2*(10 if skill==0 else min(10,reference//skill))
def required_troops(skill,reference,base):
    validate(skill,reference,base)
    ratio=f32(1.2) if skill==0 else f32(f32(reference)/f32(skill))
    return int(f32(f32(base)*max(f32(.2),min(f32(1.2),ratio))))
