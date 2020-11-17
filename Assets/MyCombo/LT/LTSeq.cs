using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LTSeq {

	public LTSeq previous;

	public LTSeq current;

	public LTDescr tween;

	public float totalDelay;

	public float timeScale;

	private int debugIter;

	public uint counter;

	public bool toggle = false;

	private uint _id;

	public int id{
		get{ 
			uint toId = _id | counter << 16;

			
			return (int)toId;
		}
	}

	public void reset(){
		previous = null;
		tween = null;
		totalDelay = 0f;
	}

	public void init(uint id, uint global_counter){
		reset();
		_id = id;

		counter = global_counter;

		this.current = this;
	}

	private LTSeq addOn(){
		this.current.toggle = true;
		LTSeq lastCurrent = this.current;
		this.current = LeanTween.sequence(true);
		Debug.Log("this.current:" + this.current.id + " lastCurrent:" + lastCurrent.id);
		this.current.previous = lastCurrent;
		lastCurrent.toggle = false;
		this.current.totalDelay = lastCurrent.totalDelay;
		this.current.debugIter = lastCurrent.debugIter + 1;
		return current;
	}

	private float addPreviousDelays(){
//		Debug.Log("delay:"+delay+" count:"+this.current.count+" this.current.totalDelay:"+this.current.totalDelay);

		LTSeq prev = this.current.previous;

		if (prev != null && prev.tween!=null) {
            return this.current.totalDelay + prev.tween.time;
		}
        return this.current.totalDelay;
	}

	
	public LTSeq append( float delay ){
        this.current.totalDelay += delay;

		return this.current;
	}

	public LTSeq append( System.Action callback ){
		LTDescr newTween = LeanTween.delayedCall(0f, callback);
//		Debug.Log("newTween:" + newTween);
		append(newTween);

		return addOn();
	}

	public LTSeq append( System.Action<object> callback, object obj ){
		append(LeanTween.delayedCall(0f, callback).setOnCompleteParam(obj));

		return addOn();
	}

	public LTSeq append( GameObject gameObject, System.Action callback ){
		append(LeanTween.delayedCall(gameObject, 0f, callback));

		return addOn();
	}

	public LTSeq append( GameObject gameObject, System.Action<object> callback, object obj ){
		append(LeanTween.delayedCall(gameObject, 0f, callback).setOnCompleteParam(obj));

		return addOn();
	}

	
	public LTSeq append( LTDescr tween ){
		this.current.tween = tween;

//		Debug.Log("tween:" + tween + " delay:" + this.current.totalDelay);

        this.current.totalDelay = addPreviousDelays();

		tween.setDelay( this.current.totalDelay );

		return addOn();
	}

	public LTSeq insert( LTDescr tween ){
		this.current.tween = tween;

        tween.setDelay( addPreviousDelays() );

		return addOn();
	}


	public LTSeq setScale( float timeScale ){
//		Debug.Log("this.current:" + this.current.previous.debugIter+" tween:"+this.current.previous.tween);
		setScaleRecursive(this.current, timeScale, 500);

		return addOn();
	}

	private void setScaleRecursive( LTSeq seq, float timeScale, int count ){
		if (count > 0) {
			this.timeScale = timeScale;

//			Debug.Log("seq.count:" + count + " seq.tween:" + seq.tween);
			seq.totalDelay *= timeScale;
			if (seq.tween != null) {
//			Debug.Log("seq.tween.time * timeScale:" + seq.tween.time * timeScale + " seq.totalDelay:"+seq.totalDelay +" time:"+seq.tween.time+" seq.tween.delay:"+seq.tween.delay);
				if (seq.tween.time != 0f)
					seq.tween.setTime(seq.tween.time * timeScale);
				seq.tween.setDelay(seq.tween.delay * timeScale);
			}

			if (seq.previous != null)
				setScaleRecursive(seq.previous, timeScale, count - 1);
		}
	}

	public LTSeq reverse(){

		return addOn();
	}

}
