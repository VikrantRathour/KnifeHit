using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GamePlayManagerKnifeHit : MonoBehaviour {

	
	public static GamePlayManagerKnifeHit instance;
	[Header("Circle Setting")]
	public CircleKnifeHit[] circlePrefabs;
	public Bosses[] BossPrefabs;

	public Transform circleSpawnPoint;
	[Range(0f,1f)]public float circleWidthByScreen=.5f;

	[Header("Knife Setting")]
	public Knife knifePrefab;
	public Transform KnifeSpawnPoint;
	[Range(0f,1f)]public float knifeHeightByScreen=.1f;

	public GameObject ApplePrefab;
	[Header("UI Object")]
	public Text lblScore;
	public Text lblStage;
	public List<Image> stageIcons;
	public Color stageIconActiveColor;
	public Color stageIconNormalColor;

	[Header("UI Boss")]

	public GameObject bossFightStart;
	public GameObject bossFightEnd;
	public AudioClip[] bossFightStartSounds;
	public AudioClip[] bossFightEndSounds;


	[Header("GameOver Popup")]
	public GameObject gameOverView;
	public Text gameOverSocreLbl,gameOverStageLbl;
	public GameObject newBestScore;
	public AudioClip gameOverSfx;
	[Space(50)]

	string currentBossName="";
	[HideInInspector]
	public CircleKnifeHit currentCircle;
	Knife currentKnife;
	public int totalSpawnKnife;

	void Awake()
	{	
		if (instance == null) {
			instance = this;		
		}
	}
	void Start () {
		startGame ();
    }


    public void startGame()
	{
        GameManagerKnifeHit.score = 0;
        GameManagerKnifeHit.Stage = 1;
        GameManagerKnifeHit.isGameOver = false;
		setupGame ();
	}
	public void UpdateLable()
	{
		lblScore.text = GameManagerKnifeHit.score+"";
		if (GameManagerKnifeHit.Stage % 10 == 0) {
			for (int i = 0; i < stageIcons.Count-1; i++) {
				stageIcons [i].gameObject.SetActive(false);
			}
			stageIcons [stageIcons.Count-1].color = stageIconActiveColor;
			lblStage.color = stageIconActiveColor;
			lblStage.text = currentBossName;
		}
		else {
			lblStage.text = "STAGE "+ GameManagerKnifeHit.Stage;
			for (int i = 0; i < stageIcons.Count; i++) {
				stageIcons [i].gameObject.SetActive(true);
				stageIcons [i].color = GameManagerKnifeHit.Stage % stageIcons.Count <= i ? stageIconNormalColor : stageIconActiveColor;
			}
			lblStage.color = stageIconNormalColor;
		}
	}
	public void setupGame()
	{
		spawnCircle ();
		currentCircle.totalKnife = GameManagerKnifeHit.Stage;
		// currentCircle.totalKnife = 1;
		KnifeCounter.intance.setUpCounter (currentCircle.totalKnife);

		totalSpawnKnife=0;
		StartCoroutine(GenerateKnife ());
	}
	void Update () {
		if (currentKnife == null)
			return;
		if (Input.GetMouseButtonDown (0) && !currentKnife.isFire) {
			KnifeCounter.intance.setHitedKnife (totalSpawnKnife);
			currentKnife.ThrowKnife ();
			StartCoroutine(GenerateKnife ());
		}

	}
	public void spawnCircle()
	{
		GameObject tempCircle;
		if (GameManagerKnifeHit.Stage % 10 == 0) {
			Bosses b = BossPrefabs [Random.Range (0, BossPrefabs.Length)];
			tempCircle = Instantiate<CircleKnifeHit> (b.BossPrefab, circleSpawnPoint.position, Quaternion.identity, circleSpawnPoint).gameObject;
			currentBossName = "Boss : " + b.Bossname;
			UpdateLable ();
			OnBossFightStart ();
		} else {
			if (GameManagerKnifeHit.Stage > 50) {
				tempCircle = Instantiate<CircleKnifeHit> (circlePrefabs [Random.Range(11,circlePrefabs.Length-1)], circleSpawnPoint.position, Quaternion.identity, circleSpawnPoint).gameObject;
			} else {
				tempCircle = Instantiate<CircleKnifeHit> (circlePrefabs [GameManagerKnifeHit.Stage - 1], circleSpawnPoint.position, Quaternion.identity, circleSpawnPoint).gameObject;
			}
		}

		tempCircle.transform.localScale = Vector3.one;
		float circleScale = (GameManagerKnifeHit.ScreenWidth * circleWidthByScreen) / tempCircle.GetComponent<SpriteRenderer> ().bounds.size.x;
		tempCircle.transform.localScale = Vector3.one * .2f;
		LeanTween.scale (tempCircle, new Vector3 (circleScale, circleScale, circleScale), .3f).setEaseOutBounce ();
		tempCircle.transform.localScale = Vector3.one*circleScale;
		currentCircle = tempCircle.GetComponent<CircleKnifeHit>();

	}
	public IEnumerator OnBossFightStart()
	{
		bossFightStart.SetActive (true);
        SoundManagerKnifeHit.instance.PlaySingle (bossFightStartSounds[Random.Range(0,bossFightEndSounds.Length-1)],1f);
		yield return new WaitForSeconds (2f);
		bossFightStart.SetActive (false);
		setupGame ();
	}

	public IEnumerator OnBossFightEnd()
	{
		bossFightEnd.SetActive (true);
        SoundManagerKnifeHit.instance.PlaySingle (bossFightEndSounds[Random.Range(0,bossFightEndSounds.Length-1)],1f);
		yield return new WaitForSeconds (2f);
		bossFightEnd.SetActive (false);
		setupGame ();
	}
	public IEnumerator GenerateKnife()
	{
		//yield return new WaitForSeconds (0.1f);
		yield return new WaitUntil (() => {
			return KnifeSpawnPoint.childCount==0;
		});
			if (currentCircle.totalKnife > totalSpawnKnife && !GameManagerKnifeHit.isGameOver ) {
				totalSpawnKnife++;
				GameObject tempKnife;
				if (GameManagerKnifeHit.selectedKnifePrefab == null) {
					tempKnife = Instantiate<Knife> (knifePrefab, new Vector3 (KnifeSpawnPoint.position.x, KnifeSpawnPoint.position.y - 2f, KnifeSpawnPoint.position.z), Quaternion.identity, KnifeSpawnPoint).gameObject;
				} else {
					tempKnife = Instantiate<Knife> (GameManagerKnifeHit.selectedKnifePrefab, new Vector3 (KnifeSpawnPoint.position.x, KnifeSpawnPoint.position.y - 2f, KnifeSpawnPoint.position.z), Quaternion.identity, KnifeSpawnPoint).gameObject;
			
				}
				tempKnife.transform.localScale = Vector3.one;
				float knifeScale = (GameManagerKnifeHit.ScreenHeight * knifeHeightByScreen) / tempKnife.GetComponent<SpriteRenderer> ().bounds.size.y;
				tempKnife.transform.localScale = Vector3.one * knifeScale;
				LeanTween.moveLocalY (tempKnife, 0, 0.1f);
				tempKnife.name ="Knife"+totalSpawnKnife; 
				currentKnife = tempKnife.GetComponent<Knife> ();
			}

	}
	public void NextLevel()
	{
		Debug.Log ("Next Level");
		if (currentCircle != null) {
			currentCircle.destroyMeAndAllKnives ();
		}
		if (GameManagerKnifeHit.Stage % 10 == 0) {
            GameManagerKnifeHit.Stage++;
			StartCoroutine (OnBossFightEnd ());
		} else {
            GameManagerKnifeHit.Stage++;
			if (GameManagerKnifeHit.Stage % 10 == 0) {
				StartCoroutine (OnBossFightStart ());
			} else {
				Invoke ("setupGame", .3f);
			}
		}
// 		if(GameManager.Stage % 20 ==11){
// GameManager.Stage++;
// 		}
// if(GameManager.Stage==11){

// }
	}

	IEnumerator currentShowingAdsPopup;
	public void GameOver()
	{
        GameManagerKnifeHit.isGameOver = true;
		showGameOverPopup();

	}
	
	public void showGameOverPopup()
	{
		gameOverView.SetActive (true);
		gameOverSocreLbl.text = GameManagerKnifeHit.score+"";
		gameOverStageLbl.text = "Stage "+ GameManagerKnifeHit.Stage;

		if (GameManagerKnifeHit.score >= GameManagerKnifeHit.HighScore) {
            GameManagerKnifeHit.HighScore = GameManagerKnifeHit.score;
			newBestScore.SetActive (true);
		} else {
			newBestScore.SetActive (false);
		}

	}
	public void RestartGame()
	{
        SoundManagerKnifeHit.instance.PlaybtnSfx ();
        GameManagerKnifeHit.Stage = (GameManagerKnifeHit.Stage / 10) * 10 + 1;
        GameManagerKnifeHit.isGameOver = false;
		gameOverView.SetActive (false);
        GameManagerKnifeHit.score=0;
		currentCircle.destroyMeAndAllKnives ();
		setupGame();
		
		// SceneManager.LoadScene("GameScene");



		// if(SinglePlayer){
		// 	GameManager.Stage = (GameManager.Stage / 10) + 1;
			// GameManager.isGameOver = false;
		// }else if(MultiPlayer){
		// 	SceneManager.LoadScene("GameScene");
		// }
		
	}
}

[System.Serializable]
public class Bosses{
	public string Bossname;
	public CircleKnifeHit BossPrefab;
}
