"""Independent delegation research model; NOT Bannerlord or a playable mod.
Risk: DUT-002 default model, checked against two native UI values.
Timing: simplified day boundaries. Casualties/wounds/rewards supplied by
scenario evidence, not predicted native RNG. Return constraints: selected
static native branches. Negative paths are simulator policy, not engine proof.
"""
from dataclasses import dataclass, asdict
import json
import random

def risk(skill, reference=120):
    if type(skill) is not int or type(reference) is not int or min(skill, reference) < 0:
        raise ValueError("nonnegative integer skills required")
    return max(0.0, min(0.9, (reference-skill)*0.005))

def allowed_return(prisoner=False, at_sea=False, in_settlement=False, map_event=False):
    return not (prisoner or (at_sea and not in_settlement) or map_event)

@dataclass
class Campaign:
    healthy: int
    wounded: int
    gold: int
    day: int = 0
    state: str = "idle"
    duration: int = 0
    sent: int = 0
    lost: int = 0
    returned: int = 0
    commander_available: bool = True
    reward_paid: int = 0
    probability: float = 0.0
    def dispatch(self, skill, soldiers, duration):
        if self.state != "idle" or not self.commander_available:
            raise ValueError("already assigned")
        if type(soldiers) is not int or type(duration) is not int or soldiers < 1 or duration < 1 or soldiers+2 > self.healthy:
            raise ValueError("insufficient personnel or invalid duration")
        chance = risk(skill)
        self.sent = soldiers+1
        self.healthy -= self.sent
        self.duration = duration
        self.probability = chance
        self.commander_available = False
        self.state = "working"
    def advance(self, days=1):
        if type(days) is not int or days < 0:
            raise ValueError("invalid time")
        self.day += days
        if self.state == "working" and self.day >= max(0,self.duration-1):
            self.state = "returning"
    def finish(self, lost, wounded, reward, success=True, **blockers):
        if self.state != "returning" or self.day < self.duration:
            raise ValueError("not ready or already finalized")
        if not allowed_return(**blockers):
            return False
        if any(type(v) is not int or v < 0 for v in (lost,wounded,reward)) or lost+wounded > self.sent-1 or (not success and reward):
            raise ValueError("invalid outcome; commander assumed surviving and fit")
        self.lost = lost
        self.returned = self.sent-lost
        self.healthy += self.returned-wounded
        self.wounded += wounded
        self.gold += reward
        self.reward_paid = reward
        self.commander_available = True
        self.state = "completed" if success else "failed"
        return True
    def snapshot(self):
        return json.dumps(asdict(self), sort_keys=True)
    @classmethod
    def restore(cls, snapshot):
        # Trusted simulator-generated JSON only, not a Bannerlord save.
        return cls(**json.loads(snapshot))

def replay(base, active, returned):
    """Use documented scenario outcomes, validate against independently read saves."""
    s = Campaign(base["healthy"],base["wounded"],base["gold"])
    s.dispatch(86,10,6)
    assert (s.healthy,s.wounded,s.gold)==(active["healthy"],active["wounded"],active["gold"])
    s=Campaign.restore(s.snapshot())
    s.advance(5)
    assert s.state=="returning" and not s.commander_available and s.healthy==54
    s.advance(1)
    # Observed D001 journal and reward log. Not a predicted random outcome.
    assert s.finish(lost=2,wounded=5,reward=550)
    assert (s.healthy,s.wounded)==(returned["healthy"],returned["wounded"])
    residual=returned["gold"]-s.gold
    return {"model":asdict(s),"saved_gold":returned["gold"],"unmodeled_net_gold":residual,
        "result":"observed-outcome replay matched aggregate personnel; reward only, not whole economy"}

def sample_risk(skill, count=10000, seed=20261126):
    if type(count) is not int or not 0 < count <= 1000000:
        raise ValueError("invalid count")
    rng=random.Random(seed)
    probability=risk(skill)
    fails=sum(rng.random()<probability for _ in range(count))
    return {"skill":skill,"trials":count,"seed":seed,"modeled_failure_probability":probability,
        "sample_failures":fails,"sample_successes":count-fails}
