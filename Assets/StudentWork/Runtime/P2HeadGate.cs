namespace StudentWork {
// One shared neutral and rearm latch for drinking and spraying. Ordinary tracking loss preserves neutral.
public sealed class P2HeadGate {
 public bool Calibrated {get;private set;} public bool Ready {get;private set;} public float Delta {get;private set;}
 float baseline, elapsed, sum, min, max, neutralHeld; bool armed=true; string epoch;
 public void Consume(){armed=false; Ready=false; neutralHeld=0;}
 public void Read(bool valid,float pitch,float dt,string currentEpoch){
  if(epoch!=currentEpoch){epoch=currentEpoch;Calibrated=false;elapsed=sum=0;armed=true;}
  Ready=false; if(!valid){elapsed=sum=0;neutralHeld=0;return;}
  if(!Calibrated){if(elapsed==0)min=max=pitch;min=UnityEngine.Mathf.Min(min,pitch);max=UnityEngine.Mathf.Max(max,pitch);if(max-min>4){elapsed=sum=0;return;}elapsed+=dt;sum+=pitch*dt;if(elapsed>=2){baseline=sum/elapsed;Calibrated=true;}return;}
  Delta=pitch-baseline;if(!armed){neutralHeld=UnityEngine.Mathf.Abs(Delta)<4?neutralHeld+dt:0;if(neutralHeld>=.3f)armed=true;}Ready=armed;
 }
}}
