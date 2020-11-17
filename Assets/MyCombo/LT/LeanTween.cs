using UnityEngine;
using System;
using System.Collections.Generic;

public enum TweenAction{
    MOVE_X,
    MOVE_Y,
    MOVE_Z,
    MOVE_LOCAL_X,
    MOVE_LOCAL_Y,
    MOVE_LOCAL_Z,
    MOVE_CURVED,
    MOVE_CURVED_LOCAL,
    MOVE_SPLINE,
    MOVE_SPLINE_LOCAL,
    SCALE_X,
    SCALE_Y,
    SCALE_Z,
    ROTATE_X,
    ROTATE_Y,
    ROTATE_Z,
    ROTATE_AROUND,
    ROTATE_AROUND_LOCAL,
    CANVAS_ROTATEAROUND,
    CANVAS_ROTATEAROUND_LOCAL,
    CANVAS_PLAYSPRITE,
    ALPHA,
    TEXT_ALPHA,
    CANVAS_ALPHA,
    CANVASGROUP_ALPHA,
    ALPHA_VERTEX,
    COLOR,
    CALLBACK_COLOR,
    TEXT_COLOR,
    CANVAS_COLOR,
    CANVAS_MOVE_X,
    CANVAS_MOVE_Y,
    CANVAS_MOVE_Z,
    CALLBACK,
    MOVE,
    MOVE_LOCAL,
    MOVE_TO_TRANSFORM,
    ROTATE,
    ROTATE_LOCAL,
    SCALE,
    VALUE3,
    GUI_MOVE,
    GUI_MOVE_MARGIN,
    GUI_SCALE,
    GUI_ALPHA,
    GUI_ROTATE,
    DELAYED_SOUND,
    CANVAS_MOVE,
    CANVAS_SCALE,
    CANVAS_SIZEDELTA,
}

public enum LeanTweenType{
    notUsed, linear, easeOutQuad, easeInQuad, easeInOutQuad, easeInCubic, easeOutCubic, easeInOutCubic, easeInQuart, easeOutQuart, easeInOutQuart, 
    easeInQuint, easeOutQuint, easeInOutQuint, easeInSine, easeOutSine, easeInOutSine, easeInExpo, easeOutExpo, easeInOutExpo, easeInCirc, easeOutCirc, easeInOutCirc, 
    easeInBounce, easeOutBounce, easeInOutBounce, easeInBack, easeOutBack, easeInOutBack, easeInElastic, easeOutElastic, easeInOutElastic, easeSpring, easeShake, punch, once, clamp, pingPong, animationCurve
}

public class LeanTween : MonoBehaviour {

    public static bool throwErrors = true;
    public static float tau = Mathf.PI*2.0f; 
    public static float PI_DIV2 = Mathf.PI / 2.0f; 

    private static LTSeq[] sequences;

    private static LTDescr[] tweens;
    private static int[] tweensFinished;
    private static int[] tweensFinishedIds;
    private static LTDescr tween;
    private static int tweenMaxSearch = -1;
    private static int maxTweens = 400;
    private static int maxSequences = 400;
    private static int frameRendered= -1;
    private static GameObject _tweenEmpty;
    public static float dtEstimated = -1f;
    public static float dtManual;
    #if UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5
    private static float previousRealTime;
    #endif
    public static float dtActual;
    private static uint global_counter = 0;
    private static int i;
    private static int j;
    private static int finishedCnt;
    public static AnimationCurve punch = new AnimationCurve( new Keyframe(0.0f, 0.0f ), new Keyframe(0.112586f, 0.9976035f ), new Keyframe(0.3120486f, -0.1720615f ), new Keyframe(0.4316337f, 0.07030682f ), new Keyframe(0.5524869f, -0.03141804f ), new Keyframe(0.6549395f, 0.003909959f ), new Keyframe(0.770987f, -0.009817753f ), new Keyframe(0.8838775f, 0.001939224f ), new Keyframe(1.0f, 0.0f ) );
    public static AnimationCurve shake = new AnimationCurve( new Keyframe(0f, 0f), new Keyframe(0.25f, 1f), new Keyframe(0.75f, -1f), new Keyframe(1f, 0f) ) ;

    public static void init(){
        init(maxTweens);
    }

    public static int maxSearch{
        get{ 
            return tweenMaxSearch;
        }
    }

    public static int maxSimulataneousTweens{
        get {
            return maxTweens;
        }
    }
    public static int tweensRunning{
        get{ 
            int count = 0;
            for (int i = 0; i <= tweenMaxSearch; i++){
                if (tweens[i].toggle){
                    count++;
                }
            }
            return count;
        }
    }

    public static void init(int maxSimultaneousTweens ){
        init(maxSimultaneousTweens, maxSequences);
    }
        
    public static void init(int maxSimultaneousTweens, int maxSimultaneousSequences){
        if(tweens==null){
            maxTweens = maxSimultaneousTweens;
            tweens = new LTDescr[maxTweens];
            tweensFinished = new int[maxTweens];
            tweensFinishedIds = new int[maxTweens];
            _tweenEmpty = new GameObject();
            _tweenEmpty.name = "~LeanTween";
            _tweenEmpty.AddComponent(typeof(LeanTween));
            _tweenEmpty.isStatic = true;
            #if !UNITY_EDITOR
            _tweenEmpty.hideFlags = HideFlags.HideAndDontSave;
            #endif
            #if UNITY_EDITOR
            if(Application.isPlaying)
                DontDestroyOnLoad( _tweenEmpty );
            #else
            DontDestroyOnLoad( _tweenEmpty );
            #endif
            for(int i = 0; i < maxTweens; i++){
                tweens[i] = new LTDescr();
            }

            #if UNITY_5_4_OR_NEWER
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += onLevelWasLoaded54;
            #endif

            sequences = new LTSeq[ maxSimultaneousSequences ]; 

            for(int i = 0; i < maxSimultaneousSequences; i++){
                sequences[i] = new LTSeq();
            }
        }
    }

    public static void reset(){
        if(tweens!=null){
            for (int i = 0; i <= tweenMaxSearch; i++){
                if(tweens[i]!=null)
                    tweens[i].toggle = false;
            }
        }
        tweens = null;
        Destroy(_tweenEmpty);
    }

    public void Update(){
        LeanTween.update();
    }

    #if UNITY_5_4_OR_NEWER
    private static void onLevelWasLoaded54( UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode ){ internalOnLevelWasLoaded( scene.buildIndex ); }
    #else
    public void OnLevelWasLoaded( int lvl ){ internalOnLevelWasLoaded( lvl ); }
    #endif

    private static void internalOnLevelWasLoaded( int lvl ){
        // Debug.Log("reseting gui");
        LTGUI.reset();
    }

    private static int maxTweenReached;

    public static void update() {
        if(frameRendered != Time.frameCount){ // make sure update is only called once per frame
            init();

            #if UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5
            dtEstimated = Time.realtimeSinceStartup - previousRealTime;
            if(dtEstimated>0.2f) // a catch put in, when at the start sometimes this number can grow unrealistically large
            dtEstimated = 0.2f;
            previousRealTime = Time.realtimeSinceStartup;
            #else

            dtEstimated = dtEstimated<0f ? 0f : dtEstimated = Time.unscaledDeltaTime;

            //      Debug.Log("Time.unscaledDeltaTime:"+Time.unscaledDeltaTime);
            #endif

            dtActual = Time.deltaTime;
            maxTweenReached = 0;
            finishedCnt = 0;
            // if(tweenMaxSearch>1500)
            //           Debug.Log("tweenMaxSearch:"+tweenMaxSearch +" maxTweens:"+maxTweens);
            for( int i = 0; i <= tweenMaxSearch && i < maxTweens; i++){
                tween = tweens[i];
//              if(i==0 && tweens[i].toggle)
//                  Debug.Log("tweens["+i+"]"+tweens[i]);
                if(tween.toggle){
                    maxTweenReached = i;

                    if (tween.updateInternal()) { // returns true if the tween is finished with it's loop
                        tweensFinished[finishedCnt] = i;
                        tweensFinishedIds[finishedCnt] = tweens[i].id;
                        finishedCnt++;
                    }
                }
            }

            // Debug.Log("maxTweenReached:"+maxTweenReached);
            tweenMaxSearch = maxTweenReached;
            frameRendered = Time.frameCount;

            for(int i = 0; i < finishedCnt; i++){
                j = tweensFinished[i];
                tween = tweens[ j ];

                if (tween.id == tweensFinishedIds[i]){
                    //              Debug.Log("removing tween:"+tween);
                    removeTween(j);
                    if(tween.hasExtraOnCompletes && tween.trans!=null)
                        tween.callOnCompletes();
                }
            }

        }
    }



    public static void removeTween( int i, int uniqueId){ // Only removes the tween if the unique id matches <summary>Move a GameObject to a certain location</summary>
        if(tweens[i].uniqueId==uniqueId){
            removeTween( i );
        }
    }

    // This method is only used internally! Do not call this from your scripts. To cancel a tween use LeanTween.cancel
    public static void removeTween( int i ){
        if(tweens[i].toggle){
            tweens[i].toggle = false;
            tweens[i].counter = uint.MaxValue;
            //logError("Removing tween["+i+"]:"+tweens[i]);
            if(tweens[i].destroyOnComplete){
//              Debug.Log("destroying tween.type:"+tween.type+" ltRect"+(tweens[i]._optional.ltRect==null));
                if(tweens[i]._optional.ltRect!=null){
                    //  Debug.Log("destroy i:"+i+" id:"+tweens[i].ltRect.id);
                    LTGUI.destroy( tweens[i]._optional.ltRect.id );
                }else{ // check if equal to tweenEmpty
                    if(tweens[i].trans!=null && tweens[i].trans.gameObject!=_tweenEmpty){
                        Destroy(tweens[i].trans.gameObject);
                    }
                }
            }
            //tweens[i].optional = null;
            startSearch = i;
            //Debug.Log("start search reset:"+startSearch + " i:"+i+" tweenMaxSearch:"+tweenMaxSearch);
            if(i+1>=tweenMaxSearch){
                //Debug.Log("reset to zero");
                startSearch = 0;
                //tweenMaxSearch--;
            }
        }
    }

    public static Vector3[] add(Vector3[] a, Vector3 b){
        Vector3[] c = new Vector3[ a.Length ];
        for(i=0; i<a.Length; i++){
            c[i] = a[i] + b;
        }

        return c;
    }

    public static float closestRot( float from, float to ){
        float minusWhole = 0 - (360 - to);
        float plusWhole = 360 + to;
        float toDiffAbs = Mathf.Abs( to-from );
        float minusDiff = Mathf.Abs(minusWhole-from);
        float plusDiff = Mathf.Abs(plusWhole-from);
        if( toDiffAbs < minusDiff && toDiffAbs < plusDiff ){
            return to;
        }else {
            if(minusDiff < plusDiff){
                return minusWhole;
            }else{
                return plusWhole;
            }
        }
    }

    
    public static void cancelAll(){
        cancelAll(false);
    }
    public static void cancelAll(bool callComplete){
        init();
        for (int i = 0; i <= tweenMaxSearch; i++)
        {
            if (tweens[i].trans != null){
                if (callComplete && tweens[i].optional.onComplete != null)
                    tweens[i].optional.onComplete();
                removeTween(i);
            }
        }
    }

   
    public static void cancel( GameObject gameObject ){
        cancel( gameObject, false);
    }
    public static void cancel( GameObject gameObject, bool callOnComplete ){
        init();
        Transform trans = gameObject.transform;
        for(int i = 0; i <= tweenMaxSearch; i++){
            if(tweens[i].toggle && tweens[i].trans==trans){
                if (callOnComplete && tweens[i].optional.onComplete != null)
                    tweens[i].optional.onComplete();
                removeTween(i);
            }
        }
    }

    public static void cancel( RectTransform rect ){
        cancel( rect.gameObject, false);
    }

//  

    public static void cancel( GameObject gameObject, int uniqueId, bool callOnComplete = false ){
        if(uniqueId>=0){
            init();
            int backId = uniqueId & 0xFFFF;
            int backCounter = uniqueId >> 16;
                // Debug.Log("uniqueId:"+uniqueId+ " id:"+backId +" counter:"+backCounter + " setCounter:"+ tw     eens[backId].counter + " tweens[id].type:"+tweens[backId].type);
            if(tweens[backId].trans==null || (tweens[backId].trans.gameObject == gameObject && tweens[backId].counter==backCounter)) {
                if (callOnComplete && tweens[backId].optional.onComplete != null)
                    tweens[backId].optional.onComplete();
                removeTween((int)backId);
            }
        }
    }

    public static void cancel( LTRect ltRect, int uniqueId ){
        if(uniqueId>=0){
            init();
            int backId = uniqueId & 0xFFFF;
            int backCounter = uniqueId >> 16;
            // Debug.Log("uniqueId:"+uniqueId+ " id:"+backId +" action:"+(TweenAction)backType + " tweens[id].type:"+tweens[backId].type);
            if(tweens[backId]._optional.ltRect == ltRect && tweens[backId].counter==backCounter)
                removeTween((int)backId);
        }
    }

    
    public static void cancel( int uniqueId ){
        cancel( uniqueId, false);
    }
    public static void cancel( int uniqueId, bool callOnComplete ){
        if(uniqueId>=0){
            init();
            int backId = uniqueId & 0xFFFF;
            int backCounter = uniqueId >> 16;
            if (backId > tweens.Length - 1) { // sequence
                int sequenceId = backId - tweens.Length;
                LTSeq seq = sequences[sequenceId];
                for (int i = 0; i < maxSequences; i++) {
                    if (seq.current.tween != null) {
                        int tweenId = seq.current.tween.uniqueId;
                        int tweenIndex = tweenId & 0xFFFF;
                        removeTween(tweenIndex);
                    }
                    if (seq.previous == null)
                        break;
                    seq.current = seq.previous;
                }
            } else { // tween
                // Debug.Log("uniqueId:"+uniqueId+ " id:"+backId +" action:"+(TweenAction)backType + " tweens[id].type:"+tweens[backId].type);
                if (tweens[backId].counter == backCounter) {
                    if (callOnComplete && tweens[backId].optional.onComplete != null)
                        tweens[backId].optional.onComplete();
                    removeTween((int)backId);
                }
            }
        }
    }

      public static LTDescr descr( int uniqueId ){
        init();

        int backId = uniqueId & 0xFFFF;
        int backCounter = uniqueId >> 16;

//      Debug.Log("backId:" + backId+" backCounter:"+backCounter);
        if (tweens[backId] != null && tweens[backId].uniqueId == uniqueId && tweens[backId].counter == backCounter) {
            // Debug.Log("tween count:" + tweens[backId].counter);
            return tweens[backId];
        }
        for(int i = 0; i <= tweenMaxSearch; i++){
            if (tweens[i].uniqueId == uniqueId && tweens[i].counter == backCounter) {
                return tweens[i];
            }
        }
        return null;
    }

    public static LTDescr description( int uniqueId ){
        return descr( uniqueId );
    }

   
    public static LTDescr[] descriptions(GameObject gameObject = null) {
        if (gameObject == null) return null;

        List<LTDescr> descrs = new List<LTDescr>();
        Transform trans = gameObject.transform;
        for (int i = 0; i <= tweenMaxSearch; i++) {
            if (tweens[i].toggle && tweens[i].trans == trans)
                descrs.Add( tweens[i] );
        }
        return descrs.ToArray();
    }

    [System.Obsolete("Use 'pause( id )' instead")]
    public static void pause( GameObject gameObject, int uniqueId ){
        pause( uniqueId );
    }

        public static void pause( int uniqueId ){
        int backId = uniqueId & 0xFFFF;
        int backCounter = uniqueId >> 16;
        if(tweens[backId].counter==backCounter){
            tweens[backId].pause();
        }
    }

    public static void pause( GameObject gameObject ){
        Transform trans = gameObject.transform;
        for(int i = 0; i <= tweenMaxSearch; i++){
            if(tweens[i].trans==trans){
                tweens[i].pause();
            }
        }
    }

   
    public static void pauseAll(){
        init();
        for (int i = 0; i <= tweenMaxSearch; i++){
            tweens[i].pause();
        }
    }

    
    public static void resumeAll(){
        init();
        for (int i = 0; i <= tweenMaxSearch; i++){
            tweens[i].resume();
        }
    }

    [System.Obsolete("Use 'resume( id )' instead")]
    public static void resume( GameObject gameObject, int uniqueId ){
        resume( uniqueId );
    }

    
    public static void resume( int uniqueId ){
        int backId = uniqueId & 0xFFFF;
        int backCounter = uniqueId >> 16;
        if(tweens[backId].counter==backCounter){
            tweens[backId].resume();
        }
    }

    
    public static void resume( GameObject gameObject ){
        Transform trans = gameObject.transform;
        for(int i = 0; i <= tweenMaxSearch; i++){
            if(tweens[i].trans==trans)
                tweens[i].resume();
        }
    }

   
    public static bool isTweening( GameObject gameObject = null ){
        if(gameObject==null){
            for(int i = 0; i <= tweenMaxSearch; i++){
                if(tweens[i].toggle)
                    return true;
            }
            return false;
        }
        Transform trans = gameObject.transform;
        for(int i = 0; i <= tweenMaxSearch; i++){
            if(tweens[i].toggle && tweens[i].trans==trans)
                return true;
        }
        return false;
    }

    public static bool isTweening( RectTransform rect ){
        return isTweening(rect.gameObject);
    }

        public static bool isTweening( int uniqueId ){
        int backId = uniqueId & 0xFFFF;
        int backCounter = uniqueId >> 16;
        if (backId < 0 || backId >= maxTweens) return false;
        // Debug.Log("tweens[backId].counter:"+tweens[backId].counter+" backCounter:"+backCounter +" toggle:"+tweens[backId].toggle);
        if(tweens[backId].counter==backCounter && tweens[backId].toggle){
            return true;
        }
        return false;
    }

    public static bool isTweening( LTRect ltRect ){
        for( int i = 0; i <= tweenMaxSearch; i++){
            if(tweens[i].toggle && tweens[i]._optional.ltRect==ltRect)
                return true;
        }
        return false;
    }

    public static void drawBezierPath(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float arrowSize = 0.0f, Transform arrowTransform = null){
        Vector3 last = a;
        Vector3 p;
        Vector3 aa = (-a + 3*(b-c) + d);
        Vector3 bb = 3*(a+c) - 6*b;
        Vector3 cc = 3*(b-a);

        float t;

        if(arrowSize>0.0f){
            Vector3 beforePos = arrowTransform.position;
            Quaternion beforeQ = arrowTransform.rotation;
            float distanceTravelled = 0f;

            for(float k = 1.0f; k <= 120.0f; k++){
                t = k / 120.0f;
                p = ((aa* t + (bb))* t + cc)* t + a;
                Gizmos.DrawLine(last, p);
                distanceTravelled += (p-last).magnitude;
                if(distanceTravelled>1f){
                    distanceTravelled = distanceTravelled - 1f;
                    /*float deltaY = p.y - last.y;
                    float deltaX = p.x - last.x;
                    float ang = Mathf.Atan(deltaY / deltaX);
                    Vector3 arrow = p + new Vector3( Mathf.Cos(ang+2.5f), Mathf.Sin(ang+2.5f), 0f)*0.5f;
                    Gizmos.DrawLine(p, arrow);
                    arrow = p + new Vector3( Mathf.Cos(ang+-2.5f), Mathf.Sin(ang+-2.5f), 0f)*0.5f;
                    Gizmos.DrawLine(p, arrow);*/

                    arrowTransform.position = p;
                    arrowTransform.LookAt( last, Vector3.forward );
                    Vector3 to = arrowTransform.TransformDirection(Vector3.right);
                    // Debug.Log("to:"+to+" tweenEmpty.transform.position:"+arrowTransform.position);
                    Vector3 back = (last-p);
                    back = back.normalized;
                    Gizmos.DrawLine(p, p + (to + back)*arrowSize);
                    to = arrowTransform.TransformDirection(-Vector3.right);
                    Gizmos.DrawLine(p, p + (to + back)*arrowSize);
                }
                last = p;
            }

            arrowTransform.position = beforePos;
            arrowTransform.rotation = beforeQ;
        }else{
            for(float k = 1.0f; k <= 30.0f; k++){
                t = k / 30.0f;
                p = ((aa* t + (bb))* t + cc)* t + a;
                Gizmos.DrawLine(last, p);
                last = p;
            }
        }
    }

    public static object logError( string error ){
        if(throwErrors) Debug.LogError(error); else Debug.Log(error);
        return null;
    }

    public static LTDescr options(LTDescr seed){ Debug.LogError("error this function is no longer used"); return null; }
    public static LTDescr options(){
        init();

        bool found = false;
        //      Debug.Log("Search start");
        for(j=0, i = startSearch; j <= maxTweens; i++){
            if(j >= maxTweens)
                return logError("LeanTween - You have run out of available spaces for tweening. To avoid this error increase the number of spaces to available for tweening when you initialize the LeanTween class ex: LeanTween.init( "+(maxTweens*2)+" );") as LTDescr;
            if(i>=maxTweens)
                i = 0;
            //          Debug.Log("searching i:"+i);
            if(tweens[i].toggle==false){
                if(i+1>tweenMaxSearch)
                    tweenMaxSearch = i+1;
                startSearch = i + 1;
                found = true;
                break;
            }

            j++;
        }
        if(found==false)
            logError("no available tween found!");

        // Debug.Log("new tween with i:"+i+" counter:"+tweens[i].counter+" tweenMaxSearch:"+tweenMaxSearch+" tween:"+tweens[i]);
        tweens[i].reset();

        global_counter++;
        if(global_counter>0x8000)
            global_counter = 0;
        
        tweens[i].setId( (uint)i, global_counter );

        return tweens[i];
    }


    public static GameObject tweenEmpty{
        get{
            init(maxTweens);
            return _tweenEmpty;
        }
    }

    public static int startSearch = 0;
    public static LTDescr d;

    private static LTDescr pushNewTween( GameObject gameObject, Vector3 to, float time, LTDescr tween ){
        init(maxTweens);
        if(gameObject==null || tween==null)
            return null;

        tween.trans = gameObject.transform;
        tween.to = to;
        tween.time = time;

        if (tween.time <= 0f)
            tween.updateInternal();
        //tween.hasPhysics = gameObject.rigidbody!=null;

        return tween;
    }

    #if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_0_1 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3 && !UNITY_4_5
   
    public static LTDescr play(RectTransform rectTransform, UnityEngine.Sprite[] sprites){
        float defaultFrameRate = 0.25f;
        float time = defaultFrameRate * sprites.Length;
        return pushNewTween(rectTransform.gameObject, new Vector3((float)sprites.Length - 1.0f,0,0), time, options().setCanvasPlaySprite().setSprites( sprites ).setRepeat(-1));
    }
    #endif

  
    public static LTDescr alpha(GameObject gameObject, float to, float time){
        LTDescr lt = pushNewTween( gameObject, new Vector3(to,0,0), time, options().setAlpha() );

        #if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_0_1 && !UNITY_4_1 && !UNITY_4_2
        SpriteRenderer ren = gameObject.GetComponent<SpriteRenderer>();
        lt.spriteRen = ren;
        #endif
        return lt;
    }

   
    public static LTSeq sequence( bool initSequence = true){
        init(maxTweens);
        // Loop through and find available sequence
        for (int i = 0; i < sequences.Length; i++) {
//          Debug.Log("i:" + i + " sequences[i]:" + sequences[i]);
            if (sequences[i].tween==null || sequences[i].tween.toggle == false) {
                if (sequences[i].toggle == false) {
                    LTSeq seq = sequences[i];
                    if (initSequence) {
                        seq.init((uint)(i + tweens.Length), global_counter);

                        global_counter++;
                        if (global_counter > 0x8000)
                            global_counter = 0;
                    } else {
                        seq.reset();
                    }
                
                    return seq;
                }
            }
        }

        return null;
    }


  
    public static LTDescr alpha(LTRect ltRect, float to, float time){
        ltRect.alphaEnabled = true;
        return pushNewTween( tweenEmpty, new Vector3(to,0f,0f), time, options().setGUIAlpha().setRect( ltRect ) );
    }


    #if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_0_1 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3 && !UNITY_4_5
    
    public static LTDescr textAlpha(RectTransform rectTransform, float to, float time){
        return pushNewTween(rectTransform.gameObject, new Vector3(to,0,0), time, options().setTextAlpha());
    }
    public static LTDescr alphaText(RectTransform rectTransform, float to, float time){
        return pushNewTween(rectTransform.gameObject, new Vector3(to,0,0), time, options().setTextAlpha());
    }

    
    public static LTDescr alphaCanvas(CanvasGroup canvasGroup, float to, float time){
        return pushNewTween(canvasGroup.gameObject, new Vector3(to,0,0), time, options().setCanvasGroupAlpha());
    }
    #endif

      public static LTDescr alphaVertex(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0f,0f), time, options().setAlphaVertex() );
    }

   
    public static LTDescr color(GameObject gameObject, Color to, float time){
        LTDescr lt = pushNewTween( gameObject, new Vector3(1.0f, to.a, 0.0f), time, options().setColor().setPoint( new Vector3(to.r, to.g, to.b) ) );
        #if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_0_1 && !UNITY_4_1 && !UNITY_4_2
        SpriteRenderer ren = gameObject.GetComponent<SpriteRenderer>();
        lt.spriteRen = ren;
        #endif
        return lt;
    }

    #if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_0_1 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3 && !UNITY_4_5
   
    public static LTDescr textColor(RectTransform rectTransform, Color to, float time){
        return pushNewTween(rectTransform.gameObject, new Vector3(1.0f, to.a, 0.0f), time, options().setTextColor().setPoint(new Vector3(to.r, to.g, to.b)));
    }
    public static LTDescr colorText(RectTransform rectTransform, Color to, float time){
        return pushNewTween(rectTransform.gameObject, new Vector3(1.0f, to.a, 0.0f), time, options().setTextColor().setPoint(new Vector3(to.r, to.g, to.b)));
    }
    #endif

   
    public static LTDescr delayedCall( float delayTime, Action callback){
        return pushNewTween( tweenEmpty, Vector3.zero, delayTime, options().setCallback().setOnComplete(callback) );
    }

    public static LTDescr delayedCall( float delayTime, Action<object> callback){
        return pushNewTween( tweenEmpty, Vector3.zero, delayTime, options().setCallback().setOnComplete(callback) );
    }

    public static LTDescr delayedCall( GameObject gameObject, float delayTime, Action callback){
        return pushNewTween( gameObject, Vector3.zero, delayTime, options().setCallback().setOnComplete(callback) );
    }

    public static LTDescr delayedCall( GameObject gameObject, float delayTime, Action<object> callback){
        return pushNewTween( gameObject, Vector3.zero, delayTime, options().setCallback().setOnComplete(callback) );
    }

    public static LTDescr destroyAfter( LTRect rect, float delayTime){
        return pushNewTween( tweenEmpty, Vector3.zero, delayTime, options().setCallback().setRect( rect ).setDestroyOnComplete(true) );
    }

    
    public static LTDescr move(GameObject gameObject, Vector3 to, float time){
        return pushNewTween( gameObject, to, time, options().setMove() );
    }
    public static LTDescr move(GameObject gameObject, Vector2 to, float time){
        return pushNewTween( gameObject, new Vector3(to.x, to.y, gameObject.transform.position.z), time, options().setMove() );
    }


   
    public static LTDescr move(GameObject gameObject, Vector3[] to, float time){
        d = options().setMoveCurved();
        if(d.optional.path==null)
            d.optional.path = new LTBezierPath( to );
        else 
            d.optional.path.setPoints( to );

        return pushNewTween( gameObject, new Vector3(1.0f,0.0f,0.0f), time, d );
    }

    public static LTDescr move(GameObject gameObject, LTBezierPath to, float time) {
        d = options().setMoveCurved();
        d.optional.path = to;

        return pushNewTween(gameObject, new Vector3(1.0f, 0.0f, 0.0f), time, d);
    }

    public static LTDescr move(GameObject gameObject, LTSpline to, float time) {
        d = options().setMoveSpline();
        d.optional.spline = to;

        return pushNewTween(gameObject, new Vector3(1.0f, 0.0f, 0.0f), time, d);
    }

    
    public static LTDescr moveSpline(GameObject gameObject, Vector3[] to, float time){
        d = options().setMoveSpline();
        d.optional.spline = new LTSpline( to );

        return pushNewTween( gameObject, new Vector3(1.0f,0.0f,0.0f), time, d );
    }

   
    public static LTDescr moveSpline(GameObject gameObject, LTSpline to, float time){
        d = options().setMoveSpline();
        d.optional.spline = to;

        return pushNewTween( gameObject, new Vector3(1.0f,0.0f,0.0f), time, d );
    }

   
    public static LTDescr moveSplineLocal(GameObject gameObject, Vector3[] to, float time){
        d = options().setMoveSplineLocal();
        d.optional.spline = new LTSpline( to );

        return pushNewTween( gameObject, new Vector3(1.0f,0.0f,0.0f), time, d );
    }

   
    public static LTDescr move(LTRect ltRect, Vector2 to, float time){
        return pushNewTween( tweenEmpty, to, time, options().setGUIMove().setRect( ltRect ) );
    }

    public static LTDescr moveMargin(LTRect ltRect, Vector2 to, float time){
        return pushNewTween( tweenEmpty, to, time, options().setGUIMoveMargin().setRect( ltRect ) );
    }

   
    public static LTDescr moveX(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setMoveX() );
    }

   
    public static LTDescr moveY(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setMoveY() );
    }

  
    public static LTDescr moveZ(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setMoveZ() );
    }

   
    public static LTDescr moveLocal(GameObject gameObject, Vector3 to, float time){
        return pushNewTween( gameObject, to, time, options().setMoveLocal() );
    }

    
    public static LTDescr moveLocal(GameObject gameObject, Vector3[] to, float time){
        d = options().setMoveCurvedLocal();
        if(d.optional.path==null)
            d.optional.path = new LTBezierPath( to );
        else 
            d.optional.path.setPoints( to );

        return pushNewTween( gameObject, new Vector3(1.0f,0.0f,0.0f), time, d );
    }

    public static LTDescr moveLocalX(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setMoveLocalX() );
    }

    public static LTDescr moveLocalY(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setMoveLocalY() );
    }

    public static LTDescr moveLocalZ(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setMoveLocalZ() );
    }

    public static LTDescr moveLocal(GameObject gameObject, LTBezierPath to, float time) {
        d = options().setMoveCurvedLocal();
        d.optional.path = to;

        return pushNewTween(gameObject, new Vector3(1.0f, 0.0f, 0.0f), time, d);
    }
    public static LTDescr moveLocal(GameObject gameObject, LTSpline to, float time) {
        d = options().setMoveSplineLocal();
        d.optional.spline = to;

        return pushNewTween(gameObject, new Vector3(1.0f, 0.0f, 0.0f), time, d);
    }

   
    public static LTDescr move(GameObject gameObject, Transform to, float time){
        return pushNewTween(gameObject, Vector3.zero, time, options().setTo(to).setMoveToTransform() );
    }

    

    public static LTDescr rotate(GameObject gameObject, Vector3 to, float time){
        return pushNewTween( gameObject, to, time, options().setRotate() );
    }

    
    public static LTDescr rotate(LTRect ltRect, float to, float time){
        return pushNewTween( tweenEmpty, new Vector3(to,0f,0f), time, options().setGUIRotate().setRect( ltRect ) );
    }

   
    public static LTDescr rotateLocal(GameObject gameObject, Vector3 to, float time){
        return pushNewTween( gameObject, to, time, options().setRotateLocal() );
    }

    
    public static LTDescr rotateX(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setRotateX() );
    }

    
    public static LTDescr rotateY(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setRotateY() );
    }

   
    public static LTDescr rotateZ(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setRotateZ() );
    }

    
    public static LTDescr rotateAround(GameObject gameObject, Vector3 axis, float add, float time){
        return pushNewTween( gameObject, new Vector3(add,0f,0f), time, options().setAxis(axis).setRotateAround() );
    }

      public static LTDescr rotateAroundLocal(GameObject gameObject, Vector3 axis, float add, float time){
        return pushNewTween( gameObject, new Vector3(add,0f,0f), time, options().setRotateAroundLocal().setAxis(axis) );
    }

   
    public static LTDescr scale(GameObject gameObject, Vector3 to, float time){
        return pushNewTween( gameObject, to, time, options().setScale() );
    }

    public static LTDescr scale(LTRect ltRect, Vector2 to, float time){
        return pushNewTween( tweenEmpty, to, time, options().setGUIScale().setRect( ltRect ) );
    }

   
    public static LTDescr scaleX(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setScaleX() );
    }

    
    public static LTDescr scaleY(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setScaleY() );
    }

  
    public static LTDescr scaleZ(GameObject gameObject, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setScaleZ());
    }
    public static LTDescr value(GameObject gameObject, float from, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setCallback().setFrom( new Vector3(from,0,0) ) );
    }
    public static LTDescr value(float from, float to, float time){
        return pushNewTween( tweenEmpty, new Vector3(to,0,0), time, options().setCallback().setFrom( new Vector3(from,0,0) ) );
    }

 
    public static LTDescr value(GameObject gameObject, Vector2 from, Vector2 to, float time){
        return pushNewTween( gameObject, new Vector3(to.x,to.y,0), time, options().setValue3().setTo( new Vector3(to.x,to.y,0f) ).setFrom( new Vector3(from.x,from.y,0) ) );
    }

    public static LTDescr value(GameObject gameObject, Vector3 from, Vector3 to, float time){
        return pushNewTween( gameObject, to, time, options().setValue3().setFrom( from ) );
    }

        public static LTDescr value(GameObject gameObject, Color from, Color to, float time){
        LTDescr lt = pushNewTween( gameObject, new Vector3(1f, to.a, 0f), time, options().setCallbackColor().setPoint( new Vector3(to.r, to.g, to.b) )
            .setFromColor(from).setHasInitialized(false) );

        #if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_0_1 && !UNITY_4_1 && !UNITY_4_2
        SpriteRenderer ren = gameObject.GetComponent<SpriteRenderer>();
        lt.spriteRen = ren;
        #endif
        return lt;
    }

      public static LTDescr value(GameObject gameObject, Action<float> callOnUpdate, float from, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setCallback().setTo( new Vector3(to,0,0) ).setFrom( new Vector3(from,0,0) ).setOnUpdate(callOnUpdate) );
    }

   

    public static LTDescr value(GameObject gameObject, Action<float, float> callOnUpdateRatio, float from, float to, float time) {
        return pushNewTween(gameObject, new Vector3(to, 0, 0), time, options().setCallback().setTo(new Vector3(to, 0, 0)).setFrom(new Vector3(from, 0, 0)).setOnUpdateRatio(callOnUpdateRatio));
    }


    public static LTDescr value(GameObject gameObject, Action<Color> callOnUpdate, Color from, Color to, float time){
        return pushNewTween( gameObject, new Vector3(1.0f,to.a,0.0f), time, options().setCallbackColor().setPoint( new Vector3(to.r, to.g, to.b) )
            .setAxis( new Vector3(from.r, from.g, from.b) ).setFrom( new Vector3(0.0f, from.a, 0.0f) ).setHasInitialized(false).setOnUpdateColor(callOnUpdate) );
    }
    public static LTDescr value(GameObject gameObject, Action<Color,object> callOnUpdate, Color from, Color to, float time){
        return pushNewTween( gameObject, new Vector3(1.0f,to.a,0.0f), time, options().setCallbackColor().setPoint( new Vector3(to.r, to.g, to.b) )
            .setAxis( new Vector3(from.r, from.g, from.b) ).setFrom( new Vector3(0.0f, from.a, 0.0f) ).setHasInitialized(false).setOnUpdateColor(callOnUpdate) );
    }

   
    public static LTDescr value(GameObject gameObject, Action<Vector2> callOnUpdate, Vector2 from, Vector2 to, float time){
        return pushNewTween( gameObject, new Vector3(to.x,to.y,0f), time, options().setValue3().setTo( new Vector3(to.x,to.y,0f) ).setFrom( new Vector3(from.x,from.y,0f) ).setOnUpdateVector2(callOnUpdate) );
    }

    
    public static LTDescr value(GameObject gameObject, Action<Vector3> callOnUpdate, Vector3 from, Vector3 to, float time){
        return pushNewTween( gameObject, to, time, options().setValue3().setTo( to ).setFrom( from ).setOnUpdateVector3(callOnUpdate) );
    }

   
    public static LTDescr value(GameObject gameObject, Action<float,object> callOnUpdate, float from, float to, float time){
        return pushNewTween( gameObject, new Vector3(to,0,0), time, options().setCallback().setTo( new Vector3(to,0,0) ).setFrom( new Vector3(from,0,0) ).setOnUpdate(callOnUpdate, gameObject) );
    }

    public static LTDescr delayedSound( AudioClip audio, Vector3 pos, float volume ){
        //Debug.LogError("Delay sound??");
        return pushNewTween( tweenEmpty, pos, 0f, options().setDelayedSound().setTo( pos ).setFrom( new Vector3(volume,0,0) ).setAudio( audio ) );
    }

    public static LTDescr delayedSound( GameObject gameObject, AudioClip audio, Vector3 pos, float volume ){
        //Debug.LogError("Delay sound??");
        return pushNewTween( gameObject, pos, 0f, options().setDelayedSound().setTo( pos ).setFrom( new Vector3(volume,0,0) ).setAudio( audio ) );
    }

    #if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_0_1 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3 && !UNITY_4_5

    
    public static LTDescr move(RectTransform rectTrans, Vector3 to, float time){
        return pushNewTween( rectTrans.gameObject, to, time, options().setCanvasMove().setRect( rectTrans ) );
    }

    
    public static LTDescr moveX(RectTransform rectTrans, float to, float time){
        return pushNewTween( rectTrans.gameObject, new Vector3(to,0f,0f), time, options().setCanvasMoveX().setRect( rectTrans ) );
    }

    
    public static LTDescr moveY(RectTransform rectTrans, float to, float time){
        return pushNewTween( rectTrans.gameObject, new Vector3(to,0f,0f), time, options().setCanvasMoveY().setRect( rectTrans ) );
    }

    
    public static LTDescr moveZ(RectTransform rectTrans, float to, float time){
        return pushNewTween( rectTrans.gameObject, new Vector3(to,0f,0f), time, options().setCanvasMoveZ().setRect( rectTrans ) );
    }

    
    public static LTDescr rotate(RectTransform rectTrans, float to, float time){
        return pushNewTween( rectTrans.gameObject, new Vector3(to,0f,0f), time, options().setCanvasRotateAround().setRect( rectTrans ).setAxis(Vector3.forward) );
    }

    public static LTDescr rotate(RectTransform rectTrans, Vector3 to, float time){
        return pushNewTween( rectTrans.gameObject, to, time, options().setCanvasRotateAround().setRect( rectTrans ).setAxis(Vector3.forward) );
    }

    
    public static LTDescr rotateAround(RectTransform rectTrans, Vector3 axis, float to, float time){
        return pushNewTween( rectTrans.gameObject, new Vector3(to,0f,0f), time, options().setCanvasRotateAround().setRect( rectTrans ).setAxis(axis) );
    }

    
    public static LTDescr rotateAroundLocal(RectTransform rectTrans, Vector3 axis, float to, float time){
        return pushNewTween( rectTrans.gameObject, new Vector3(to,0f,0f), time, options().setCanvasRotateAroundLocal().setRect( rectTrans ).setAxis(axis) );
    }

   
    public static LTDescr scale(RectTransform rectTrans, Vector3 to, float time){
        return pushNewTween( rectTrans.gameObject, to, time, options().setCanvasScale().setRect( rectTrans ) );
    }

  
    public static LTDescr size(RectTransform rectTrans, Vector2 to, float time){
        return pushNewTween( rectTrans.gameObject, to, time, options().setCanvasSizeDelta().setRect( rectTrans ) );
    }

    
    public static LTDescr alpha(RectTransform rectTrans, float to, float time){
        return pushNewTween( rectTrans.gameObject, new Vector3(to,0f,0f), time, options().setCanvasAlpha().setRect( rectTrans ) );
    }

   
    public static LTDescr color(RectTransform rectTrans, Color to, float time){
        return pushNewTween( rectTrans.gameObject, new Vector3(1.0f, to.a, 0.0f), time, options().setCanvasColor().setRect( rectTrans ).setPoint( new Vector3(to.r, to.g, to.b) ) );
    }

    #endif

    // Tweening Functions - Thanks to Robert Penner and GFX47

    public static float tweenOnCurve( LTDescr tweenDescr, float ratioPassed ){
        // Debug.Log("single ratio:"+ratioPassed+" tweenDescr.animationCurve.Evaluate(ratioPassed):"+tweenDescr.animationCurve.Evaluate(ratioPassed));
        return tweenDescr.from.x + (tweenDescr.diff.x) * tweenDescr.optional.animationCurve.Evaluate(ratioPassed);
    }

    public static Vector3 tweenOnCurveVector( LTDescr tweenDescr, float ratioPassed ){
        return  new Vector3(tweenDescr.from.x + (tweenDescr.diff.x) * tweenDescr.optional.animationCurve.Evaluate(ratioPassed),
            tweenDescr.from.y + (tweenDescr.diff.y) * tweenDescr.optional.animationCurve.Evaluate(ratioPassed),
            tweenDescr.from.z + (tweenDescr.diff.z) * tweenDescr.optional.animationCurve.Evaluate(ratioPassed) );
    }

    public static float easeOutQuadOpt( float start, float diff, float ratioPassed ){
        return -diff * ratioPassed * (ratioPassed - 2) + start;
    }

    public static float easeInQuadOpt( float start, float diff, float ratioPassed ){
        return diff * ratioPassed * ratioPassed + start;
    }

    public static float easeInOutQuadOpt( float start, float diff, float ratioPassed ){
        ratioPassed /= .5f;
        if (ratioPassed < 1) return diff / 2 * ratioPassed * ratioPassed + start;
        ratioPassed--;
        return -diff / 2 * (ratioPassed * (ratioPassed - 2) - 1) + start;
    }

    public static Vector3 easeInOutQuadOpt( Vector3 start, Vector3 diff, float ratioPassed ){
        ratioPassed /= .5f;
        if (ratioPassed < 1) return diff / 2 * ratioPassed * ratioPassed + start;
        ratioPassed--;
        return -diff / 2 * (ratioPassed * (ratioPassed - 2) - 1) + start;
    }

    public static float linear(float start, float end, float val){
        return Mathf.Lerp(start, end, val);
    }

    public static float clerp(float start, float end, float val){
        float min = 0.0f;
        float max = 360.0f;
        float half = Mathf.Abs((max - min) / 2.0f);
        float retval = 0.0f;
        float diff = 0.0f;
        if ((end - start) < -half){
            diff = ((max - start) + end) * val;
            retval = start + diff;
        }else if ((end - start) > half){
            diff = -((max - end) + start) * val;
            retval = start + diff;
        }else retval = start + (end - start) * val;
        return retval;
    }

    public static float spring(float start, float end, float val ){
        val = Mathf.Clamp01(val);
        val = (Mathf.Sin(val * Mathf.PI * (0.2f + 2.5f * val * val * val)) * Mathf.Pow(1f - val, 2.2f ) + val) * (1f + (1.2f * (1f - val) ));
        return start + (end - start) * val;
    }

    public static float easeInQuad(float start, float end, float val){
        end -= start;
        return end * val * val + start;
    }

    public static float easeOutQuad(float start, float end, float val){
        end -= start;
        return -end * val * (val - 2) + start;
    }

    public static float easeInOutQuad(float start, float end, float val){
        val /= .5f;
        end -= start;
        if (val < 1) return end / 2 * val * val + start;
        val--;
        return -end / 2 * (val * (val - 2) - 1) + start;
    }


    public static float easeInOutQuadOpt2(float start, float diffBy2, float val, float val2){
        val /= .5f;
        if (val < 1) return diffBy2 * val2 + start;
        val--;
        return -diffBy2 * ((val2 - 2) - 1f) + start;
    }

    public static float easeInCubic(float start, float end, float val){
        end -= start;
        return end * val * val * val + start;
    }

    public static float easeOutCubic(float start, float end, float val){
        val--;
        end -= start;
        return end * (val * val * val + 1) + start;
    }

    public static float easeInOutCubic(float start, float end, float val){
        val /= .5f;
        end -= start;
        if (val < 1) return end / 2 * val * val * val + start;
        val -= 2;
        return end / 2 * (val * val * val + 2) + start;
    }

    public static float easeInQuart(float start, float end, float val){
        end -= start;
        return end * val * val * val * val + start;
    }

    public static float easeOutQuart(float start, float end, float val){
        val--;
        end -= start;
        return -end * (val * val * val * val - 1) + start;
    }

    public static float easeInOutQuart(float start, float end, float val){
        val /= .5f;
        end -= start;
        if (val < 1) return end / 2 * val * val * val * val + start;
        val -= 2;
        return -end / 2 * (val * val * val * val - 2) + start;
    }

    public static float easeInQuint(float start, float end, float val){
        end -= start;
        return end * val * val * val * val * val + start;
    }

    public static float easeOutQuint(float start, float end, float val){
        val--;
        end -= start;
        return end * (val * val * val * val * val + 1) + start;
    }

    public static float easeInOutQuint(float start, float end, float val){
        val /= .5f;
        end -= start;
        if (val < 1) return end / 2 * val * val * val * val * val + start;
        val -= 2;
        return end / 2 * (val * val * val * val * val + 2) + start;
    }

    public static float easeInSine(float start, float end, float val){
        end -= start;
        return -end * Mathf.Cos(val / 1 * (Mathf.PI / 2)) + end + start;
    }

    public static float easeOutSine(float start, float end, float val){
        end -= start;
        return end * Mathf.Sin(val / 1 * (Mathf.PI / 2)) + start;
    }

    public static float easeInOutSine(float start, float end, float val){
        end -= start;
        return -end / 2 * (Mathf.Cos(Mathf.PI * val / 1) - 1) + start;
    }

    public static float easeInExpo(float start, float end, float val){
        end -= start;
        return end * Mathf.Pow(2, 10 * (val / 1 - 1)) + start;
    }

    public static float easeOutExpo(float start, float end, float val){
        end -= start;
        return end * (-Mathf.Pow(2, -10 * val / 1) + 1) + start;
    }

    public static float easeInOutExpo(float start, float end, float val){
        val /= .5f;
        end -= start;
        if (val < 1) return end / 2 * Mathf.Pow(2, 10 * (val - 1)) + start;
        val--;
        return end / 2 * (-Mathf.Pow(2, -10 * val) + 2) + start;
    }

    public static float easeInCirc(float start, float end, float val){
        end -= start;
        return -end * (Mathf.Sqrt(1 - val * val) - 1) + start;
    }

    public static float easeOutCirc(float start, float end, float val){
        val--;
        end -= start;
        return end * Mathf.Sqrt(1 - val * val) + start;
    }

    public static float easeInOutCirc(float start, float end, float val){
        val /= .5f;
        end -= start;
        if (val < 1) return -end / 2 * (Mathf.Sqrt(1 - val * val) - 1) + start;
        val -= 2;
        return end / 2 * (Mathf.Sqrt(1 - val * val) + 1) + start;
    }

    public static float easeInBounce(float start, float end, float val){
        end -= start;
        float d = 1f;
        return end - easeOutBounce(0, end, d-val) + start;
    }

    public static float easeOutBounce(float start, float end, float val){
        val /= 1f;
        end -= start;
        if (val < (1 / 2.75f)){
            return end * (7.5625f * val * val) + start;
        }else if (val < (2 / 2.75f)){
            val -= (1.5f / 2.75f);
            return end * (7.5625f * (val) * val + .75f) + start;
        }else if (val < (2.5 / 2.75)){
            val -= (2.25f / 2.75f);
            return end * (7.5625f * (val) * val + .9375f) + start;
        }else{
            val -= (2.625f / 2.75f);
            return end * (7.5625f * (val) * val + .984375f) + start;
        }
    }

    public static float easeInOutBounce(float start, float end, float val){
        end -= start;
        float d= 1f;
        if (val < d/2) return easeInBounce(0, end, val*2) * 0.5f + start;
        else return easeOutBounce(0, end, val*2-d) * 0.5f + end*0.5f + start;
    }

    public static float easeInBack(float start, float end, float val, float overshoot = 1.0f){
        end -= start;
        val /= 1;
        float s= 1.70158f * overshoot;
        return end * (val) * val * ((s + 1) * val - s) + start;
    }

    public static float easeOutBack(float start, float end, float val, float overshoot = 1.0f){
        float s = 1.70158f * overshoot;
        end -= start;
        val = (val / 1) - 1;
        return end * ((val) * val * ((s + 1) * val + s) + 1) + start;
    }

    public static float easeInOutBack(float start, float end, float val, float overshoot = 1.0f){
        float s = 1.70158f * overshoot;
        end -= start;
        val /= .5f;
        if ((val) < 1){
            s *= (1.525f) * overshoot;
            return end / 2 * (val * val * (((s) + 1) * val - s)) + start;
        }
        val -= 2;
        s *= (1.525f) * overshoot;
        return end / 2 * ((val) * val * (((s) + 1) * val + s) + 2) + start;
    }

    public static float easeInElastic(float start, float end, float val, float overshoot = 1.0f, float period = 0.3f){
        end -= start;

        float p = period;
        float s = 0f;
        float a = 0f;

        if (val == 0f) return start;

        if (val == 1f) return start + end;

        if (a == 0f || a < Mathf.Abs(end)){
            a = end;
            s = p / 4f;
        }else{
            s = p / (2f * Mathf.PI) * Mathf.Asin(end / a);
        }

        if(overshoot>1f && val>0.6f )
            overshoot = 1f + ((1f-val) / 0.4f * (overshoot-1f));
        // Debug.Log("ease in elastic val:"+val+" a:"+a+" overshoot:"+overshoot);

        val = val-1f;
        return start-(a * Mathf.Pow(2f, 10f * val) * Mathf.Sin((val - s) * (2f * Mathf.PI) / p)) * overshoot;
    }       

    public static float easeOutElastic(float start, float end, float val, float overshoot = 1.0f, float period = 0.3f){
        end -= start;

        float p = period;
        float s = 0f;
        float a = 0f;

        if (val == 0f) return start;

        // Debug.Log("ease out elastic val:"+val+" a:"+a);
        if (val == 1f) return start + end;

        if (a == 0f || a < Mathf.Abs(end)){
            a = end;
            s = p / 4f;
        }else{
            s = p / (2f * Mathf.PI) * Mathf.Asin(end / a);
        }
        if(overshoot>1f && val<0.4f )
            overshoot = 1f + (val / 0.4f * (overshoot-1f));
        // Debug.Log("ease out elastic val:"+val+" a:"+a+" overshoot:"+overshoot);

        return start + end + a * Mathf.Pow(2f, -10f * val) * Mathf.Sin((val - s) * (2f * Mathf.PI) / p) * overshoot;
    }       

    public static float easeInOutElastic(float start, float end, float val, float overshoot = 1.0f, float period = 0.3f)
    {
        end -= start;

        float p = period;
        float s = 0f;
        float a = 0f;

        if (val == 0f) return start;

        val = val / (1f/2f);
        if (val == 2f) return start + end;

        if (a == 0f || a < Mathf.Abs(end)){
            a = end;
            s = p / 4f;
        }else{
            s = p / (2f * Mathf.PI) * Mathf.Asin(end / a);
        }

        if(overshoot>1f){
            if( val<0.2f ){
                overshoot = 1f + (val / 0.2f * (overshoot-1f));
            }else if( val > 0.8f ){
                overshoot = 1f + ((1f-val) / 0.2f * (overshoot-1f));
            }
        }

        if (val < 1f){
            val = val-1f;
            return start - 0.5f * (a * Mathf.Pow(2f, 10f * val) * Mathf.Sin((val - s) * (2f * Mathf.PI) / p)) * overshoot;
        }
        val = val-1f;
        return end + start + a * Mathf.Pow(2f, -10f * val) * Mathf.Sin((val - s) * (2f * Mathf.PI) / p) * 0.5f * overshoot;
    }

    // LeanTween Listening/Dispatch

    private static System.Action<LTEvent>[] eventListeners;
    private static GameObject[] goListeners;
    private static int eventsMaxSearch = 0;
    public static int EVENTS_MAX = 10;
    public static int LISTENERS_MAX = 10;
    private static int INIT_LISTENERS_MAX = LISTENERS_MAX;

    public static void addListener( int eventId, System.Action<LTEvent> callback ){
        addListener(tweenEmpty, eventId, callback);
    }

   
    public static void addListener( GameObject caller, int eventId, System.Action<LTEvent> callback ){
        if(eventListeners==null){
            INIT_LISTENERS_MAX = LISTENERS_MAX;
            eventListeners = new System.Action<LTEvent>[ EVENTS_MAX * LISTENERS_MAX ];
            goListeners = new GameObject[ EVENTS_MAX * LISTENERS_MAX ];
        }
        // Debug.Log("searching for an empty space for:"+caller + " eventid:"+event);
        for(i = 0; i < INIT_LISTENERS_MAX; i++){
            int point = eventId*INIT_LISTENERS_MAX + i;
            if(goListeners[ point ]==null || eventListeners[ point ]==null){
                eventListeners[ point ] = callback;
                goListeners[ point ] = caller;
                if(i>=eventsMaxSearch)
                    eventsMaxSearch = i+1;
                // Debug.Log("adding event for:"+caller.name);

                return;
            }
            #if UNITY_FLASH
            if(goListeners[ point ] == caller && System.Object.ReferenceEquals( eventListeners[ point ], callback)){  
            // Debug.Log("This event is already being listened for.");
            return;
            }
            #else
            if(goListeners[ point ] == caller && System.Object.Equals( eventListeners[ point ], callback)){  
                // Debug.Log("This event is already being listened for.");
                return;
            }
            #endif
        }
        Debug.LogError("You ran out of areas to add listeners, consider increasing LISTENERS_MAX, ex: LeanTween.LISTENERS_MAX = "+(LISTENERS_MAX*2));
    }

    public static bool removeListener( int eventId, System.Action<LTEvent> callback ){
        return removeListener( tweenEmpty, eventId, callback);
    }

    public static bool removeListener( int eventId ){
        int point = eventId*INIT_LISTENERS_MAX + i;
        eventListeners[ point ] = null;
        goListeners[ point ] = null;
        return true;
    }


    
    public static bool removeListener( GameObject caller, int eventId, System.Action<LTEvent> callback ){
        for(i = 0; i < eventsMaxSearch; i++){
            int point = eventId*INIT_LISTENERS_MAX + i;
            #if UNITY_FLASH
            if(goListeners[ point ] == caller && System.Object.ReferenceEquals( eventListeners[ point ], callback) ){
            #else
            if(goListeners[ point ] == caller && System.Object.Equals( eventListeners[ point ], callback) ){
            #endif
                eventListeners[ point ] = null;
                goListeners[ point ] = null;
                return true;
            }
        }
        return false;
    }

   
    public static void dispatchEvent( int eventId ){
        dispatchEvent( eventId, null);
    }

   
    public static void dispatchEvent( int eventId, object data ){
        for(int k = 0; k < eventsMaxSearch; k++){
            int point = eventId*INIT_LISTENERS_MAX + k;
            if(eventListeners[ point ]!=null){
                if(goListeners[point]){
                    eventListeners[ point ]( new LTEvent(eventId, data) );
                }else{
                    eventListeners[ point ] = null;
                }
            }
        }
    }


} // End LeanTween class

public class LTUtility {

    public static Vector3[] reverse( Vector3[] arr ){
        int length = arr.Length;
        int left = 0;
        int right = length - 1;

        for (; left < right; left += 1, right -= 1){
            Vector3 temporary = arr[left];
            arr[left] = arr[right];
            arr[right] = temporary;
        }
        return arr;
    }
}

public class LTBezier {
    public float length;

    private Vector3 a;
    private Vector3 aa;
    private Vector3 bb;
    private Vector3 cc;
    private float len;
    private float[] arcLengths;

    public LTBezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float precision){
        this.a = a;
        aa = (-a + 3*(b-c) + d);
        bb = 3*(a+c) - 6*b;
        cc = 3*(b-a);

        this.len = 1.0f / precision;
        arcLengths = new float[(int)this.len + (int)1];
        arcLengths[0] = 0;

        Vector3 ov = a;
        Vector3 v;
        float clen = 0.0f;
        for(int i = 1; i <= this.len; i++) {
            v = bezierPoint(i * precision);
            clen += (ov - v).magnitude;
            this.arcLengths[i] = clen;
            ov = v;
        }
        this.length = clen;
    }

    private float map(float u) {
        float targetLength = u * this.arcLengths[(int)this.len];
        int low = 0;
        int high = (int)this.len;
        int index = 0;
        while (low < high) {
            index = low + ((int)((high - low) / 2.0f) | 0);
            if (this.arcLengths[index] < targetLength) {
                low = index + 1;
            } else {
                high = index;
            }
        }
        if(this.arcLengths[index] > targetLength)
            index--;
        if(index<0)
            index = 0;

        return (index + (targetLength - arcLengths[index]) / (arcLengths[index + 1] - arcLengths[index])) / this.len;
    }

    private Vector3 bezierPoint(float t){
        return ((aa* t + (bb))* t + cc)* t + a;
    }

    public Vector3 point(float t){ 
        return bezierPoint( map(t) ); 
    }
}


public class LTBezierPath {
    public Vector3[] pts;
    public float length;
    public bool orientToPath;
    public bool orientToPath2d;

    private LTBezier[] beziers;
    private float[] lengthRatio;
    private int currentBezier=0,previousBezier=0;

    public LTBezierPath(){ }
    public LTBezierPath( Vector3[] pts_ ){
        setPoints( pts_ );
    }

    public void setPoints( Vector3[] pts_ ){
        if(pts_.Length<4)
            LeanTween.logError( "LeanTween - When passing values for a vector path, you must pass four or more values!" );
        if(pts_.Length%4!=0)
            LeanTween.logError( "LeanTween - When passing values for a vector path, they must be in sets of four: controlPoint1, controlPoint2, endPoint2, controlPoint2, controlPoint2..." );

        pts = pts_;

        int k = 0;
        beziers = new LTBezier[ pts.Length / 4 ];
        lengthRatio = new float[ beziers.Length ];
        int i;
        length = 0;
        for(i = 0; i < pts.Length; i+=4){
            beziers[k] = new LTBezier(pts[i+0],pts[i+2],pts[i+1],pts[i+3],0.05f);
            length += beziers[k].length;
            k++;
        }
        // Debug.Log("beziers.Length:"+beziers.Length + " beziers:"+beziers);
        for(i = 0; i < beziers.Length; i++){
            lengthRatio[i] = beziers[i].length / length;
        }
    }

    /**
    * @property {float} distance distance of the path (in unity units)
    */
    public float distance{
        get{
            return length;
        }
    }

   
    public Vector3 point( float ratio ){
        float added = 0.0f;
        for(int i = 0; i < lengthRatio.Length; i++){
            added += lengthRatio[i];
            if(added >= ratio)
                return beziers[i].point( (ratio-(added-lengthRatio[i])) / lengthRatio[i] );
        }
        return beziers[lengthRatio.Length-1].point( 1.0f );
    }

    public void place2d( Transform transform, float ratio ){
        transform.position = point( ratio );
        ratio += 0.001f;
        if(ratio<=1.0f){
            Vector3 v3Dir = point( ratio ) - transform.position;
            float angle = Mathf.Atan2(v3Dir.y, v3Dir.x) * Mathf.Rad2Deg;
            transform.eulerAngles = new Vector3(0, 0, angle);
        }
    }

    public void placeLocal2d( Transform transform, float ratio ){
        transform.localPosition = point( ratio );
        ratio += 0.001f;
        if(ratio<=1.0f){
            Vector3 v3Dir = point( ratio ) - transform.localPosition;
            float angle = Mathf.Atan2(v3Dir.y, v3Dir.x) * Mathf.Rad2Deg;
            transform.localEulerAngles = new Vector3(0, 0, angle);
        }
    }

   
    public void place( Transform transform, float ratio ){
        place( transform, ratio, Vector3.up );

    }

   
    public void place( Transform transform, float ratio, Vector3 worldUp ){
        transform.position = point( ratio );
        ratio += 0.001f;
        if(ratio<=1.0f)
            transform.LookAt( point( ratio ), worldUp );

    }

   
    public void placeLocal( Transform transform, float ratio ){
        placeLocal( transform, ratio, Vector3.up );
    }

   
    public void placeLocal( Transform transform, float ratio, Vector3 worldUp ){
        // Debug.Log("place ratio:" + ratio + " greater:"+(ratio>1f));
        ratio = Mathf.Clamp01(ratio);
        transform.localPosition = point( ratio );
        // Debug.Log("ratio:" + ratio + " +:" + (ratio + 0.001f));
        ratio = Mathf.Clamp01(ratio + 0.001f);

        if(ratio<=1.0f)
            transform.LookAt( transform.parent.TransformPoint( point( ratio ) ), worldUp );
    }

    public void gizmoDraw(float t = -1.0f)
    {
        Vector3 prevPt = point(0);

        for (int i = 1; i <= 120; i++)
        {
            float pm = (float)i / 120f;
            Vector3 currPt2 = point(pm);
            //Gizmos.color = new Color(UnityEngine.Random.Range(0f,1f),UnityEngine.Random.Range(0f,1f),UnityEngine.Random.Range(0f,1f),1);
            Gizmos.color = (previousBezier == currentBezier) ? Color.magenta : Color.grey;
            Gizmos.DrawLine(currPt2, prevPt);
            prevPt = currPt2;
            previousBezier = currentBezier;
        }
    }
}


[System.Serializable]
public class LTSpline {
    public static int DISTANCE_COUNT = 3; // increase for a more accurate constant speed
    public static int SUBLINE_COUNT = 20; // increase for a more accurate smoothing of the curves into lines

    /**
    * @property {float} distance distance of the spline (in unity units)
    */
    public float distance = 0f;

    public bool constantSpeed = true;

    public Vector3[] pts;
    [System.NonSerialized]
    public Vector3[] ptsAdj;
    public int ptsAdjLength;
    public bool orientToPath;
    public bool orientToPath2d;
    private int numSections;
    private int currPt;

    public LTSpline( Vector3[] pts ){
        init( pts, true);
    }

    public LTSpline( Vector3[] pts, bool constantSpeed ) {
        this.constantSpeed = constantSpeed;
        init(pts, constantSpeed);
    }

    private void init( Vector3[] pts, bool constantSpeed){
        if(pts.Length<4){
            LeanTween.logError( "LeanTween - When passing values for a spline path, you must pass four or more values!" );
            return;
        }

        this.pts = new Vector3[pts.Length];
        System.Array.Copy(pts, this.pts, pts.Length);

        numSections = pts.Length - 3;

        float minSegment = float.PositiveInfinity;
        Vector3 earlierPoint = this.pts[1];
        float totalDistance = 0f;
        for(int i=1; i < this.pts.Length-1; i++){
            // float pointDistance = (this.pts[i]-earlierPoint).sqrMagnitude;
            float pointDistance = Vector3.Distance(this.pts[i], earlierPoint);
            //Debug.Log("pointDist:"+pointDistance);
            if(pointDistance < minSegment){
                minSegment = pointDistance;
            }

            totalDistance += pointDistance;
        }

        if(constantSpeed){
            minSegment = totalDistance / (numSections*SUBLINE_COUNT);
            //Debug.Log("minSegment:"+minSegment+" numSections:"+numSections);

            float minPrecision = minSegment / SUBLINE_COUNT; // number of subdivisions in each segment
            int precision = (int)Mathf.Ceil(totalDistance / minPrecision) * DISTANCE_COUNT;
            // Debug.Log("precision:"+precision);
            if(precision<=1) // precision has to be greater than one
                precision = 2;

            ptsAdj = new Vector3[ precision ];
            earlierPoint = interp( 0f );
            int num = 1;
            ptsAdj[0] = earlierPoint;
            distance = 0f;
            for(int i = 0; i < precision + 1; i++){
                float fract = ((float)(i)) / precision;
                // Debug.Log("fract:"+fract);
                Vector3 point = interp( fract );
                float dist = Vector3.Distance(point, earlierPoint);

                // float dist = (point-earlierPoint).sqrMagnitude;
                if(dist>=minPrecision || fract>=1.0f){
                    ptsAdj[num] = point;
                    distance += dist; // only add it to the total distance once we know we are adding it as an adjusted point

                    earlierPoint = point;
                    // Debug.Log("fract:"+fract+" point:"+point);
                    num++;
                }
            }
          
            ptsAdjLength = num;
        }
        // Debug.Log("map 1f:"+map(1f)+" end:"+ptsAdj[ ptsAdjLength-1 ]);

        // Debug.Log("ptsAdjLength:"+ptsAdjLength+" minPrecision:"+minPrecision+" precision:"+precision);
    }

    public Vector3 map( float u ){
        if(u>=1f)
            return pts[ pts.Length - 2];
        float t = u * (ptsAdjLength-1);
        int first = (int)Mathf.Floor( t );
        int next = (int)Mathf.Ceil( t );

        if(first<0)
            first = 0;

        Vector3 val = ptsAdj[ first ];


        Vector3 nextVal = ptsAdj[ next ];
        float diff = t - first;

        // Debug.Log("u:"+u+" val:"+val +" nextVal:"+nextVal+" diff:"+diff+" first:"+first+" next:"+next);

        val = val + (nextVal - val) * diff;

        return val;
    }

    public Vector3 interp(float t) {
        currPt = Mathf.Min(Mathf.FloorToInt(t * (float) numSections), numSections - 1);
        float u = t * (float) numSections - (float) currPt;

        //Debug.Log("currPt:"+currPt+" numSections:"+numSections+" pts.Length :"+pts.Length );
        Vector3 a = pts[currPt];
        Vector3 b = pts[currPt + 1];
        Vector3 c = pts[currPt + 2];
        Vector3 d = pts[currPt + 3];

        Vector3 val = (.5f * (
            (-a + 3f * b - 3f * c + d) * (u * u * u)
            + (2f * a - 5f * b + 4f * c - d) * (u * u)
            + (-a + c) * u
            + 2f * b));
        // Debug.Log("currPt:"+currPt+" t:"+t+" val.x"+val.x+" y:"+val.y+" z:"+val.z);

        return val;
    }

   
    public float ratioAtPoint( Vector3 pt ){
        float closestDist = float.MaxValue;
        int closestI = 0;
        for (int i = 0; i < ptsAdjLength; i++) {
            float dist = Vector3.Distance(pt, ptsAdj[i]);
            // Debug.Log("i:"+i+" dist:"+dist);
            if(dist<closestDist){
                closestDist = dist;
                closestI = i;
            }
        }
        // Debug.Log("closestI:"+closestI+" ptsAdjLength:"+ptsAdjLength);
        return (float) closestI / (float)(ptsAdjLength-1);
    }

   
    public Vector3 point( float ratio ){
        float t = ratio>1f?1f:ratio;
        return constantSpeed ? map(t) : interp(t);
    }

    public void place2d( Transform transform, float ratio ){
        transform.position = point( ratio );
        ratio += 0.001f;
        if(ratio<=1.0f){
            Vector3 v3Dir = point( ratio ) - transform.position;
            float angle = Mathf.Atan2(v3Dir.y, v3Dir.x) * Mathf.Rad2Deg;
            transform.eulerAngles = new Vector3(0, 0, angle);
        }
    }

    public void placeLocal2d( Transform transform, float ratio ){
        Transform trans = transform.parent;
        if(trans==null){ // this has no parent, just do a regular transform
            place2d(transform, ratio);
            return;
        }
        transform.localPosition = point( ratio );
        ratio += 0.001f;
        if(ratio<=1.0f){
            Vector3 ptAhead = point( ratio );//trans.TransformPoint(  );
            Vector3 v3Dir =  ptAhead - transform.localPosition;
            float angle = Mathf.Atan2(v3Dir.y, v3Dir.x) * Mathf.Rad2Deg;
            transform.localEulerAngles = new Vector3(0, 0, angle);
        }
    }


   
    public void place( Transform transform, float ratio ){
        place(transform, ratio, Vector3.up);
    }

   
    public void place( Transform transform, float ratio, Vector3 worldUp ){
        // ratio = Mathf.Repeat(ratio, 1.0f); // make sure ratio is always between 0-1
        transform.position = point( ratio );
        ratio += 0.001f;
        if(ratio<=1.0f)
            transform.LookAt( point( ratio ), worldUp );

    }

  
    public void placeLocal( Transform transform, float ratio ){
        placeLocal( transform, ratio, Vector3.up );
    }

   
    public void placeLocal( Transform transform, float ratio, Vector3 worldUp ){
        transform.localPosition = point( ratio );
        ratio += 0.001f;
        if(ratio<=1.0f)
            transform.LookAt( transform.parent.TransformPoint( point( ratio ) ), worldUp );
    }

    public void gizmoDraw(float t = -1.0f) {
        if(ptsAdj==null || ptsAdj.Length<=0)
            return;

        Vector3 prevPt = ptsAdj[0];

        for (int i = 0; i < ptsAdjLength; i++) {
            Vector3 currPt2 = ptsAdj[i];
            // Debug.Log("currPt2:"+currPt2);
            //Gizmos.color = new Color(UnityEngine.Random.Range(0f,1f),UnityEngine.Random.Range(0f,1f),UnityEngine.Random.Range(0f,1f),1);
            Gizmos.DrawLine(prevPt, currPt2);
            prevPt = currPt2;
        }
    }

    public void drawGizmo( Color color ) {
        if( this.ptsAdjLength>=4){

            Vector3 prevPt = this.ptsAdj[0];

            Color colorBefore = Gizmos.color;
            Gizmos.color = color;
            for (int i = 0; i < this.ptsAdjLength; i++) {
                Vector3 currPt2 = this.ptsAdj[i];
                // Debug.Log("currPt2:"+currPt2);

                Gizmos.DrawLine(prevPt, currPt2);
                prevPt = currPt2;
            }
            Gizmos.color = colorBefore;
        }
    }

    public static void drawGizmo(Transform[] arr, Color color) {
        if(arr.Length>=4){
            Vector3[] vec3s = new Vector3[arr.Length];
            for(int i = 0; i < arr.Length; i++){
                vec3s[i] = arr[i].position;
            }
            LTSpline spline = new LTSpline(vec3s);
            Vector3 prevPt = spline.ptsAdj[0];

            Color colorBefore = Gizmos.color;
            Gizmos.color = color;
            for (int i = 0; i < spline.ptsAdjLength; i++) {
                Vector3 currPt2 = spline.ptsAdj[i];
                // Debug.Log("currPt2:"+currPt2);

                Gizmos.DrawLine(prevPt, currPt2);
                prevPt = currPt2;
            }
            Gizmos.color = colorBefore;
        }
    }


    public static void drawLine(Transform[] arr, float width, Color color) {
        if(arr.Length>=4){

        }
    }

    public void drawLinesGLLines(Material outlineMaterial, Color color, float width){
        GL.PushMatrix();
        outlineMaterial.SetPass(0);
        GL.LoadPixelMatrix();
        GL.Begin(GL.LINES);
        GL.Color(color);

        if (constantSpeed) {
            if (this.ptsAdjLength >= 4) {

                Vector3 prevPt = this.ptsAdj[0];

                for (int i = 0; i < this.ptsAdjLength; i++) {
                    Vector3 currPt2 = this.ptsAdj[i];
                    GL.Vertex(prevPt);
                    GL.Vertex(currPt2);

                    prevPt = currPt2;
                }
            }

        } else {
            if (this.pts.Length >= 4) {

                Vector3 prevPt = this.pts[0];

                float split = 1f / ((float)this.pts.Length * 10f);

                float iter = 0f;
                while (iter < 1f) {
                    float at = iter / 1f;
                    Vector3 currPt2 = interp(at);
                    // Debug.Log("currPt2:"+currPt2);

                    GL.Vertex(prevPt);
                    GL.Vertex(currPt2);

                    prevPt = currPt2;

                    iter += split;
                }
            }
        }


        GL.End();
        GL.PopMatrix();

    }

    public Vector3[] generateVectors(){
        if (this.pts.Length >= 4) {
            List<Vector3> meshPoints = new List<Vector3>();
            Vector3 prevPt = this.pts[0];
            meshPoints.Add(prevPt);

            float split = 1f / ((float)this.pts.Length * 10f);

            float iter = 0f;
            while (iter < 1f) {
                float at = iter / 1f;
                Vector3 currPt2 = interp(at);
                //                Debug.Log("currPt2:"+currPt2);

                //                GL.Vertex(prevPt);
                //                GL.Vertex(currPt2);
                meshPoints.Add(currPt2);

                //                prevPt = currPt2;

                iter += split;
            }

            meshPoints.ToArray();
        }
        return null;
    }
}



[System.Serializable]
public class LTRect : System.Object{
    /**
    * Pass this value to the GUI Methods
    * 
    * @property rect
    * @type {Rect} rect:Rect Rect object that controls the positioning and size
    */
    public Rect _rect;
    public float alpha = 1f;
    public float rotation;
    public Vector2 pivot;
    public Vector2 margin;
    public Rect relativeRect = new Rect(0f,0f,float.PositiveInfinity,float.PositiveInfinity);

    public bool rotateEnabled;
    [HideInInspector]
    public bool rotateFinished;
    public bool alphaEnabled;
    public string labelStr;
    public LTGUI.Element_Type type;
    public GUIStyle style;
    public bool useColor = false;
    public Color color = Color.white;
    public bool fontScaleToFit;
    public bool useSimpleScale;
    public bool sizeByHeight;

    public Texture texture;

    private int _id = -1;
    [HideInInspector]
    public int counter;

    public static bool colorTouched;

    public LTRect(){
        reset();
        this.rotateEnabled = this.alphaEnabled = true;
        _rect = new Rect(0f,0f,1f,1f);
    }

    public LTRect(Rect rect){
        _rect = rect;
        reset();
    }

    public LTRect(float x, float y, float width, float height){
        _rect = new Rect(x,y,width,height);
        this.alpha = 1.0f;
        this.rotation = 0.0f;
        this.rotateEnabled = this.alphaEnabled = false;
    }

    public LTRect(float x, float y, float width, float height, float alpha){
        _rect = new Rect(x,y,width,height);
        this.alpha = alpha;
        this.rotation = 0.0f;
        this.rotateEnabled = this.alphaEnabled = false;
    }

    public LTRect(float x, float y, float width, float height, float alpha, float rotation){
        _rect = new Rect(x,y,width,height);
        this.alpha = alpha;
        this.rotation = rotation;
        this.rotateEnabled = this.alphaEnabled = false;
        if(rotation!=0.0f){
            this.rotateEnabled = true;
            resetForRotation();
        }
    }

    public bool hasInitiliazed{
        get{ 
            return _id!=-1;
        }
    }

    public int id{
        get{ 
            int toId = _id | counter << 16;

         

            return toId;
        }
    } 

    public void setId( int id, int counter){
        this._id = id;
        this.counter = counter;
    }

    public void reset(){
        this.alpha = 1.0f;
        this.rotation = 0.0f;
        this.rotateEnabled = this.alphaEnabled = false;
        this.margin = Vector2.zero;
        this.sizeByHeight = false;
        this.useColor = false;
    }

    public void resetForRotation(){
        Vector3 scale = new Vector3(GUI.matrix[0,0], GUI.matrix[1,1], GUI.matrix[2,2]);
        if(pivot==Vector2.zero){
            pivot = new Vector2((_rect.x+((_rect.width)*0.5f )) * scale.x + GUI.matrix[0,3], (_rect.y+((_rect.height)*0.5f )) * scale.y + GUI.matrix[1,3]);
        }
    }

    public float x{
        get{ return _rect.x; }
        set{ _rect.x = value; }
    }

    public float y{
        get{ return _rect.y; }
        set{ _rect.y = value; }
    }

    public float width{
        get{ return _rect.width; }
        set{ _rect.width = value; }
    }

    public float height{
        get{ return _rect.height; }
        set{ _rect.height = value; }
    }

    public Rect rect{

        get{
            if(colorTouched){
                colorTouched = false;
                GUI.color = new Color(GUI.color.r,GUI.color.g,GUI.color.b,1.0f);
            }
            if(rotateEnabled){
                if(rotateFinished){
                    rotateFinished = false;
                    rotateEnabled = false;
                    //this.rotation = 0.0f;
                    pivot = Vector2.zero;
                }else{
                    GUIUtility.RotateAroundPivot(rotation, pivot);
                }
            }
            if(alphaEnabled){
                GUI.color = new Color(GUI.color.r,GUI.color.g,GUI.color.b,alpha);
                colorTouched = true;
            }
            if(fontScaleToFit){
                if(this.useSimpleScale){
                    style.fontSize = (int)(_rect.height*this.relativeRect.height);
                }else{
                    style.fontSize = (int)_rect.height;
                }
            }
            return _rect;
        }

        set{
            _rect = value;
        }   
    }

    public LTRect setStyle( GUIStyle style ){
        this.style = style;
        return this;
    }

    public LTRect setFontScaleToFit( bool fontScaleToFit ){
        this.fontScaleToFit = fontScaleToFit;
        return this;
    }

    public LTRect setColor( Color color ){
        this.color = color;
        this.useColor = true;
        return this;
    }

    public LTRect setAlpha( float alpha ){
        this.alpha = alpha;
        return this;
    }

    public LTRect setLabel( String str ){
        this.labelStr = str;
        return this;
    }

    public LTRect setUseSimpleScale( bool useSimpleScale, Rect relativeRect){
        this.useSimpleScale = useSimpleScale;
        this.relativeRect = relativeRect;
        return this;
    }

    public LTRect setUseSimpleScale( bool useSimpleScale){
        this.useSimpleScale = useSimpleScale;
        this.relativeRect = new Rect(0f,0f,Screen.width,Screen.height);
        return this;
    }

    public LTRect setSizeByHeight( bool sizeByHeight){
        this.sizeByHeight = sizeByHeight;
        return this;
    }

    public override string ToString(){
        return "x:"+_rect.x+" y:"+_rect.y+" width:"+_rect.width+" height:"+_rect.height;
    }
}


public class LTEvent {
    public int id;
    public object data;

    public LTEvent(int id, object data){
        this.id = id;
        this.data = data;
    }
}

public class LTGUI {
    public static int RECT_LEVELS = 5;
    public static int RECTS_PER_LEVEL = 10;
    public static int BUTTONS_MAX = 24;

    private static LTRect[] levels;
    private static int[] levelDepths;
    private static Rect[] buttons;
    private static int[] buttonLevels;
    private static int[] buttonLastFrame;
    private static LTRect r;
    private static Color color = Color.white;
    private static bool isGUIEnabled = false;
    private static int global_counter = 0;

    public enum Element_Type{
        Texture,
        Label
    }

    public static void init(){
        if(levels==null){
            levels = new LTRect[RECT_LEVELS*RECTS_PER_LEVEL];
            levelDepths = new int[RECT_LEVELS];
        }
    }

    public static void initRectCheck(){
        if(buttons==null){
            buttons = new Rect[BUTTONS_MAX];
            buttonLevels = new int[BUTTONS_MAX];
            buttonLastFrame = new int[BUTTONS_MAX];
            for(int i = 0; i < buttonLevels.Length; i++){
                buttonLevels[i] = -1;
            }
        }
    }

    public static void reset(){
        if(isGUIEnabled){
            isGUIEnabled = false;
            for(int i = 0; i < levels.Length; i++){
                levels[i] = null;
            }

            for(int i = 0; i < levelDepths.Length; i++){
                levelDepths[i] = 0;
            }
        }
    }

    public static void update( int updateLevel ){
        if(isGUIEnabled){
            init();
            if(levelDepths[updateLevel]>0){
                color = GUI.color;
                int baseI = updateLevel*RECTS_PER_LEVEL;
                int maxLoop = baseI + levelDepths[updateLevel];// RECTS_PER_LEVEL;//;

                for(int i = baseI; i < maxLoop; i++){
                    r = levels[i];
                    // Debug.Log("r:"+r+" i:"+i);
                    if(r!=null /*&& checkOnScreen(r.rect)*/){
                        //Debug.Log("label:"+r.labelStr+" textColor:"+r.style.normal.textColor);
                        if(r.useColor)
                            GUI.color = r.color;
                        if(r.type == Element_Type.Label){
                            if(r.style!=null)
                                GUI.skin.label = r.style;
                            if(r.useSimpleScale){
                                GUI.Label( new Rect((r.rect.x + r.margin.x + r.relativeRect.x)*r.relativeRect.width, (r.rect.y + r.margin.y + r.relativeRect.y)*r.relativeRect.height, r.rect.width*r.relativeRect.width, r.rect.height*r.relativeRect.height), r.labelStr );
                            }else{
                                GUI.Label( new Rect(r.rect.x + r.margin.x, r.rect.y + r.margin.y, r.rect.width, r.rect.height), r.labelStr );
                            }
                        }else if(r.type == Element_Type.Texture && r.texture!=null){
                            Vector2 size = r.useSimpleScale ? new Vector2(0f, r.rect.height*r.relativeRect.height) : new Vector2(r.rect.width, r.rect.height);
                            if(r.sizeByHeight){
                                size.x = (float)r.texture.width/(float)r.texture.height * size.y;
                            }
                            if(r.useSimpleScale){
                                GUI.DrawTexture( new Rect((r.rect.x + r.margin.x + r.relativeRect.x)*r.relativeRect.width, (r.rect.y + r.margin.y + r.relativeRect.y)*r.relativeRect.height, size.x, size.y), r.texture );
                            }else{
                                GUI.DrawTexture( new Rect(r.rect.x + r.margin.x, r.rect.y + r.margin.y, size.x, size.y), r.texture );
                            }
                        }
                    }
                }
                GUI.color = color;
            }
        }
    }

    public static bool checkOnScreen(Rect rect){
        bool offLeft = rect.x + rect.width < 0f;
        bool offRight = rect.x > Screen.width;
        bool offBottom = rect.y > Screen.height;
        bool offTop = rect.y + rect.height < 0f;

        return !(offLeft || offRight || offBottom || offTop);
    }

    public static void destroy( int id ){
        int backId = id & 0xFFFF;
        int backCounter = id >> 16;
        if(id>=0 && levels[backId]!=null && levels[backId].hasInitiliazed && levels[backId].counter==backCounter)
            levels[backId] = null;
    }

    public static void destroyAll( int depth ){ // clears all gui elements on depth
        int maxLoop = depth*RECTS_PER_LEVEL + RECTS_PER_LEVEL;
        for(int i = depth*RECTS_PER_LEVEL; levels!=null && i < maxLoop; i++){
            levels[i] = null;
        }
    }

    public static LTRect label( Rect rect, string label, int depth){
        return LTGUI.label(new LTRect(rect), label, depth);
    }

    public static LTRect label( LTRect rect, string label, int depth){
        rect.type = Element_Type.Label;
        rect.labelStr = label;
        return element(rect, depth);
    }

    public static LTRect texture( Rect rect, Texture texture, int depth){
        return LTGUI.texture( new LTRect(rect), texture, depth);
    }

    public static LTRect texture( LTRect rect, Texture texture, int depth){
        rect.type = Element_Type.Texture;
        rect.texture = texture;
        return element(rect, depth);
    }

    public static LTRect element( LTRect rect, int depth){
        isGUIEnabled = true;
        init();
        int maxLoop = depth*RECTS_PER_LEVEL + RECTS_PER_LEVEL;
        int k = 0;
        if(rect!=null){
            destroy(rect.id);
        }
        if(rect.type==LTGUI.Element_Type.Label && rect.style!=null){
            if(rect.style.normal.textColor.a<=0f){
                Debug.LogWarning("Your GUI normal color has an alpha of zero, and will not be rendered.");
            }
        }
        if(rect.relativeRect.width==float.PositiveInfinity){
            rect.relativeRect = new Rect(0f,0f,Screen.width,Screen.height);
        }
        for(int i = depth*RECTS_PER_LEVEL; i < maxLoop; i++){
            r = levels[i];
            if(r==null){
                r = rect;
                r.rotateEnabled = true;
                r.alphaEnabled = true;
                r.setId( i, global_counter );
                levels[i] = r;
                // Debug.Log("k:"+k+ " maxDepth:"+levelDepths[depth]);
                if(k>=levelDepths[depth]){
                    levelDepths[depth] = k + 1;
                }
                global_counter++;
                return r;
            }
            k++;
        }

        Debug.LogError("You ran out of GUI Element spaces");

        return null;
    }

    public static bool hasNoOverlap( Rect rect, int depth ){
        initRectCheck();
        bool hasNoOverlap = true;
        bool wasAddedToList = false;
        for(int i = 0; i < buttonLevels.Length; i++){
            // Debug.Log("buttonLastFrame["+i+"]:"+buttonLastFrame[i]);
            //Debug.Log("buttonLevels["+i+"]:"+buttonLevels[i]);
            if(buttonLevels[i]>=0){
                //Debug.Log("buttonLastFrame["+i+"]:"+buttonLastFrame[i]+" Time.frameCount:"+Time.frameCount);
                if( buttonLastFrame[i] + 1 < Time.frameCount ){ // It has to have been visible within the current, or
                    buttonLevels[i] = -1;
                    // Debug.Log("resetting i:"+i);
                }else{
                    //if(buttonLevels[i]>=0)
                    //   Debug.Log("buttonLevels["+i+"]:"+buttonLevels[i]);
                    if(buttonLevels[i]>depth){
                        /*if(firstTouch().x > 0){
                            Debug.Log("buttons["+i+"]:"+buttons[i] + " firstTouch:");
                            Debug.Log(firstTouch());
                            Debug.Log(buttonLevels[i]);
                        }*/
                        if(pressedWithinRect( buttons[i] )){
                            hasNoOverlap = false; // there is an overlapping button that is higher
                        }
                    }
                }
            }

            if(wasAddedToList==false && buttonLevels[i]<0){
                wasAddedToList = true;
                buttonLevels[i] = depth;
                buttons[i] = rect;
                buttonLastFrame[i] = Time.frameCount;
            }
        }

        return hasNoOverlap;
    }

    public static bool pressedWithinRect( Rect rect ){
        Vector2 vec2 = firstTouch();
        if(vec2.x<0f)
            return false;
        float vecY = Screen.height-vec2.y;
        return (vec2.x > rect.x && vec2.x < rect.x + rect.width && vecY > rect.y && vecY < rect.y + rect.height);
    }

    public static bool checkWithinRect(Vector2 vec2, Rect rect){
        vec2.y = Screen.height-vec2.y;
        return (vec2.x > rect.x && vec2.x < rect.x + rect.width && vec2.y > rect.y && vec2.y < rect.y + rect.height);
    }

    public static Vector2 firstTouch(){
        if(Input.touchCount>0){
            return Input.touches[0].position;
        }else if(Input.GetMouseButton(0)){
            return Input.mousePosition;
        }

        return new Vector2(Mathf.NegativeInfinity,Mathf.NegativeInfinity);
    }

}

namespace DentedPixel { public class LeanDummy {}  }
//}
