using UnityEngine;

public class SecondaryRevolver : MonoBehaviour
{
	private int bulletForce;

	private GameObject camObj;

	private Camera cam;

	public RaycastHit hit;

	private bool gunReady;

	public Vector3 shotHitPoint;

	private bool shootReady;

	private float shootCharge;

	private int currentGunShot;

	private AudioSource gunAud;

	public Revolver rev;

	public GameObject secBeamPoint;

	public GameObject secHitParticle;

	private GameObject gunBarrel;

	private Vector3 defaultGunPos;

	private Quaternion defaultGunRot;

	private MeshRenderer screenMR;

	private CameraFrustumTargeter targeter;

	private void Awake()
	{
		defaultGunPos = base.transform.localPosition;
		defaultGunRot = base.transform.localRotation;
		Debug.Log("Started!");
	}

	private void Start()
	{
		targeter = Camera.main.GetComponent<CameraFrustumTargeter>();
		screenMR = base.transform.GetChild(1).GetComponent<MeshRenderer>();
		gunBarrel = base.transform.GetChild(0).gameObject;
		cam = MonoSingleton<CameraController>.Instance.GetComponent<Camera>();
		camObj = cam.gameObject;
		rev = camObj.GetComponentInChildren<Revolver>();
		gunAud = GetComponent<AudioSource>();
		PickUp();
		Debug.Log("Awake!");
	}

	private void OnDisable()
	{
		PickUp();
	}

	public void PickUp()
	{
		gunReady = false;
		shootCharge = 0f;
		shootReady = false;
	}

	private void Update()
	{
		if (gunReady && !MonoSingleton<InputManager>.Instance.PerformingCheatMenuCombo() && MonoSingleton<InputManager>.Instance.InputSource.Fire1.WasPerformedThisFrame && shootReady)
		{
			Shoot();
		}
		if (base.transform.localPosition != defaultGunPos && base.transform.localRotation != defaultGunRot)
		{
			gunReady = false;
		}
		else
		{
			gunReady = true;
		}
		if (!shootReady)
		{
			if (shootCharge + 175f * Time.deltaTime < 100f)
			{
				shootCharge += 175f * Time.deltaTime;
			}
			else
			{
				shootCharge = 100f;
				shootReady = true;
			}
		}
		if (base.transform.localPosition != defaultGunPos)
		{
			base.transform.localPosition = Vector3.MoveTowards(base.transform.localPosition, defaultGunPos, Time.deltaTime * 3f);
		}
		if (base.transform.localRotation != defaultGunRot)
		{
			base.transform.localRotation = Quaternion.RotateTowards(base.transform.localRotation, defaultGunRot, Time.deltaTime * 160f);
		}
		if (shootCharge < 50f)
		{
			screenMR.material.SetTexture("_MainTex", rev.batteryLow);
		}
		else if (shootCharge < 100f)
		{
			screenMR.material.SetTexture("_MainTex", rev.batteryMid);
		}
		else
		{
			screenMR.material.SetTexture("_MainTex", rev.batteryFull);
		}
	}

	public void Shoot()
	{
		bulletForce = 5000;
		Vector3 direction = camObj.transform.forward;
		if ((bool)targeter.CurrentTarget && targeter.IsAutoAimed)
		{
			direction = targeter.CurrentTargetAimPosition - camObj.transform.position;
		}
		bool flag = PortalPhysicsV2.Raycast(camObj.transform.position, direction, float.PositiveInfinity, LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment), out var hitInfo, out var _, out var _);
		shotHitPoint = (flag ? hitInfo.point : Vector3.zero);
		GameObject gameObject = Object.Instantiate(secBeamPoint, gunBarrel.transform.position, gunBarrel.transform.rotation);
		if ((bool)targeter.CurrentTarget && targeter.IsAutoAimed)
		{
			gameObject.transform.LookAt(targeter.CurrentTargetAimPosition);
		}
		if (flag)
		{
			Object.Instantiate(secHitParticle, hitInfo.point, base.transform.rotation);
		}
		shootReady = false;
		shootCharge = 0f;
		currentGunShot = Random.Range(0, rev.gunShots.Length);
		gunAud.clip = rev.gunShots[currentGunShot];
		gunAud.volume = 0.5f;
		gunAud.SetPitch(Random.Range(0.95f, 1.05f));
		gunAud.Play(tracked: true);
		cam.fieldOfView = (rev.recoilFOV - cam.fieldOfView) / 2f + cam.fieldOfView;
	}

	private void ReadyToShoot()
	{
		shootReady = true;
	}
}
