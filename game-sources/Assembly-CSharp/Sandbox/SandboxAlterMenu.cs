using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Arm;
using TMPro;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.UI;
using plog;

namespace Sandbox;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class SandboxAlterMenu : MonoSingleton<SandboxAlterMenu>
{
	private static readonly plog.Logger Log = new plog.Logger("SandboxAlterMenu");

	[SerializeField]
	private GameObject shadow;

	[SerializeField]
	private GameObject menu;

	[Space]
	[SerializeField]
	private TMP_Text nameText;

	[Space]
	[SerializeField]
	private Toggle uniformSize;

	[SerializeField]
	private InputField sizeField;

	[SerializeField]
	private InputField sizeFieldX;

	[SerializeField]
	private InputField sizeFieldY;

	[SerializeField]
	private InputField sizeFieldZ;

	[Space]
	[SerializeField]
	private Toggle radianceEnabled;

	[SerializeField]
	private Slider radianceTier;

	[SerializeField]
	private Slider radianceHealth;

	[SerializeField]
	private Slider radianceDamage;

	[SerializeField]
	private Slider radianceSpeed;

	[Space]
	[SerializeField]
	private GameObject sizeContainer;

	[SerializeField]
	private GameObject uniformContainer;

	[SerializeField]
	private Toggle frozenCheckbox;

	[SerializeField]
	private Toggle disallowManipulationCheckbox;

	[SerializeField]
	private Toggle disallowFreezingCheckbox;

	[SerializeField]
	private GameObject splitContainer;

	[SerializeField]
	private GameObject enemyOptionsContainer;

	[SerializeField]
	private GameObject radianceSettings;

	[Space]
	[SerializeField]
	private GameObject scaleUpSound;

	[SerializeField]
	private GameObject scaleDownSound;

	[SerializeField]
	private GameObject scaleResetSound;

	[Space]
	[SerializeField]
	private AlterMenuElements elementManager;

	public SpawnableInstance editedObject;

	public AlterMode alterInstance;

	public Vector3 SafeSize(Vector3 originalSize)
	{
		float min = 1f / 256f;
		float max = 128f;
		float x = Mathf.Clamp(originalSize.x, min, max);
		float y = Mathf.Clamp(originalSize.y, min, max);
		float z = Mathf.Clamp(originalSize.z, min, max);
		return new Vector3(x, y, z);
	}

	private void Awake()
	{
		sizeFieldX.onValueChanged.AddListener(SetSizeX);
		sizeFieldY.onValueChanged.AddListener(SetSizeY);
		sizeFieldZ.onValueChanged.AddListener(SetSizeZ);
		sizeField.onValueChanged.AddListener(SetSize);
		sizeFieldX.onEndEdit.AddListener((string _) =>
		{
			UpdateSizeValues();
		});
		sizeFieldY.onEndEdit.AddListener((string _) =>
		{
			UpdateSizeValues();
		});
		sizeFieldZ.onEndEdit.AddListener((string _) =>
		{
			UpdateSizeValues();
		});
		sizeField.onEndEdit.AddListener((string _) =>
		{
			UpdateSizeValues();
		});
	}

	private void SetSizeX(string value)
	{
		if (!(editedObject == null))
		{
			Vector3 normalizedSize = editedObject.normalizedSize;
			if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				editedObject.SetSize(SafeSize(new Vector3(result, normalizedSize.y, normalizedSize.z)));
			}
		}
	}

	private void SetSizeY(string value)
	{
		if (!(editedObject == null))
		{
			Vector3 normalizedSize = editedObject.normalizedSize;
			Debug.Log(value);
			if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				editedObject.SetSize(SafeSize(new Vector3(normalizedSize.x, result, normalizedSize.z)));
			}
		}
	}

	private void SetSizeZ(string value)
	{
		if (!(editedObject == null))
		{
			Vector3 normalizedSize = editedObject.normalizedSize;
			if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				editedObject.SetSize(SafeSize(new Vector3(normalizedSize.x, normalizedSize.y, result)));
			}
		}
	}

	private void SetSize(string value)
	{
		if (!(editedObject == null) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			editedObject.SetSizeUniform(result);
		}
	}

	public void SetJumpPadPower(float value)
	{
		if (!(editedObject == null))
		{
			JumpPad componentInChildren = editedObject.GetComponentInChildren<JumpPad>();
			if (!(componentInChildren == null))
			{
				componentInChildren.force = value;
			}
		}
	}

	public void SetFrozen(bool frozen)
	{
		if ((bool)editedObject)
		{
			editedObject.frozen = frozen;
		}
	}

	public void SetDisallowManipulation(bool disallow)
	{
		if ((bool)editedObject)
		{
			editedObject.disallowManipulation = disallow;
		}
	}

	public void SetDisallowFreezing(bool disallow)
	{
		if ((bool)editedObject)
		{
			editedObject.disallowFreezing = disallow;
		}
	}

	public void SetRadianceTierSlider(float value)
	{
		SetRadianceTier(value / 2f);
	}

	public void SetRadianceTier(float value)
	{
		if (editedObject is EnemySpawnableInstance enemySpawnableInstance)
		{
			enemySpawnableInstance.radiance.tier = value;
			enemySpawnableInstance.UpdateRadiance();
		}
	}

	public void SetHealthBuffSlider(float value)
	{
		SetHealthBuff(value / 2f);
	}

	public void SetHealthBuff(float value)
	{
		if (editedObject is EnemySpawnableInstance enemySpawnableInstance)
		{
			enemySpawnableInstance.radiance.healthBuff = value;
			enemySpawnableInstance.UpdateRadiance();
		}
	}

	public void SetDamageBuffSlider(float value)
	{
		SetDamageBuff(value / 2f);
	}

	public void SetDamageBuff(float value)
	{
		if (editedObject is EnemySpawnableInstance enemySpawnableInstance)
		{
			Debug.Log("Setting Damage Buff: " + value);
			enemySpawnableInstance.radiance.damageBuff = value;
			enemySpawnableInstance.UpdateRadiance();
		}
	}

	public void SetSpeedBuffSlider(float value)
	{
		SetSpeedBuff(value / 2f);
	}

	public void SetSpeedBuff(float value)
	{
		if (editedObject is EnemySpawnableInstance enemySpawnableInstance)
		{
			enemySpawnableInstance.radiance.speedBuff = value;
			enemySpawnableInstance.UpdateRadiance();
		}
	}

	public void ShowRadianceOptions(bool value)
	{
		radianceSettings.SetActive(value);
		if (editedObject is EnemySpawnableInstance enemySpawnableInstance)
		{
			if (enemySpawnableInstance.radiance == null)
			{
				enemySpawnableInstance.radiance = new EnemyRadianceConfig(enemySpawnableInstance.eid);
			}
			enemySpawnableInstance.radiance.enabled = value;
			if (value)
			{
				Debug.Log("Loading Damage Buff: " + enemySpawnableInstance.radiance.damageBuff);
				radianceEnabled.SetIsOnWithoutNotify(value: true);
				radianceTier.SetValueWithoutNotify(enemySpawnableInstance.radiance.tier * 2f);
				radianceDamage.SetValueWithoutNotify(enemySpawnableInstance.radiance.damageBuff * 2f);
				radianceHealth.SetValueWithoutNotify(enemySpawnableInstance.radiance.healthBuff * 2f);
				radianceSpeed.SetValueWithoutNotify(enemySpawnableInstance.radiance.speedBuff * 2f);
			}
			else
			{
				radianceEnabled.SetIsOnWithoutNotify(value: false);
			}
			enemySpawnableInstance.UpdateRadiance();
		}
	}

	public void ShowUniformSizeMenu(bool value)
	{
		uniformSize.SetIsOnWithoutNotify(value);
		uniformContainer.SetActive(value);
		splitContainer.SetActive(!value);
		sizeFieldX.interactable = !value;
		sizeFieldY.interactable = !value;
		sizeFieldZ.interactable = !value;
		sizeField.interactable = value;
		if (value)
		{
			editedObject.SetSizeUniform(editedObject.normalizedSize.x);
		}
		UpdateSizeValues();
	}

	public void DefaultSize()
	{
		if (!(editedObject == null))
		{
			editedObject.transform.localScale = editedObject.defaultSize;
			UpdateSizeValues();
			UnityEngine.Object.Instantiate(scaleResetSound, base.transform.position, Quaternion.identity);
		}
	}

	public void MultiplySize(float value)
	{
		if (!(editedObject == null))
		{
			Vector3 localScale = editedObject.transform.localScale;
			localScale *= value;
			localScale = SafeSize(localScale);
			editedObject.transform.localScale = localScale;
			ShowUniformSizeMenu(uniformContainer.activeSelf);
			UpdateSizeValues();
			UnityEngine.Object.Instantiate((value > 1f) ? scaleUpSound : scaleDownSound, editedObject.transform.position, Quaternion.identity);
		}
	}

	public void UpdateSizeValues()
	{
		Vector3 localScale = editedObject.transform.localScale;
		if (uniformContainer.activeSelf)
		{
			sizeField.SetTextWithoutNotify((localScale.x / editedObject.defaultSize.x).ToString(CultureInfo.InvariantCulture));
			return;
		}
		sizeFieldX.SetTextWithoutNotify((localScale.x / editedObject.defaultSize.x).ToString(CultureInfo.InvariantCulture));
		sizeFieldY.SetTextWithoutNotify((localScale.y / editedObject.defaultSize.y).ToString(CultureInfo.InvariantCulture));
		sizeFieldZ.SetTextWithoutNotify((localScale.z / editedObject.defaultSize.z).ToString(CultureInfo.InvariantCulture));
	}

	public void Show(SpawnableInstance prop, AlterMode instance)
	{
		if (SandboxArmDebug.DebugActive)
		{
			Log.Info("Showing Sandbox Alter Menu for " + prop.name);
		}
		prop.Pause(freeze: false);
		shadow.SetActive(value: true);
		elementManager.Reset();
		menu.SetActive(value: true);
		frozenCheckbox.SetIsOnWithoutNotify(prop.frozen);
		disallowManipulationCheckbox.SetIsOnWithoutNotify(prop.disallowManipulation);
		disallowFreezingCheckbox.SetIsOnWithoutNotify(prop.disallowFreezing);
		nameText.text = prop.name;
		editedObject = prop;
		alterInstance = instance;
		GameStateManager.Instance.RegisterState(new GameState("alter-menu", menu)
		{
			cursorLock = LockMode.Unlock,
			cameraInputLock = LockMode.Lock,
			playerInputLock = LockMode.Unlock
		});
		MonoSingleton<CameraController>.Instance.activated = false;
		MonoSingleton<GunControl>.Instance.activated = false;
		bool flag = !(prop is BrushBlock);
		sizeContainer.SetActive(flag);
		if (flag)
		{
			ShowUniformSizeMenu(prop.uniformSize);
		}
		if (prop is EnemySpawnableInstance enemySpawnableInstance)
		{
			ShowRadianceOptions(enemySpawnableInstance.radiance.enabled);
			enemyOptionsContainer.SetActive(value: true);
		}
		else
		{
			enemyOptionsContainer.SetActive(value: false);
			radianceSettings.SetActive(value: false);
		}
		IAlter[] componentsInChildren = prop.GetComponentsInChildren<IAlter>();
		List<string> list = new List<string>();
		IAlter[] array = componentsInChildren;
		foreach (IAlter alter in array)
		{
			if (alter.alterKey != null)
			{
				if (list.Contains(alter.alterKey))
				{
					continue;
				}
				list.Add(alter.alterKey);
			}
			int num = 0;
			if (alter is IAlterOptions<bool> { options: not null } alterOptions)
			{
				num += alterOptions.options.Length;
			}
			if (alter is IAlterOptions<float> { options: not null } alterOptions2)
			{
				num += alterOptions2.options.Length;
			}
			if (alter is IAlterOptions<Vector3> { options: not null } alterOptions3)
			{
				num += alterOptions3.options.Length;
			}
			if (alter is IAlterOptions<int> { options: not null } alterOptions4)
			{
				num += alterOptions4.options.Length;
			}
			if (num == 0)
			{
				continue;
			}
			elementManager.CreateTitle(alter.alterCategoryName ?? alter.alterKey ?? string.Empty);
			int num2 = 0;
			if (alter is IAlterOptions<bool> alterOptions5)
			{
				if (alterOptions5.options == null)
				{
					continue;
				}
				AlterOption<bool>[] options = alterOptions5.options;
				foreach (AlterOption<bool> alterOption in options)
				{
					if (!alterOption.hidden)
					{
						elementManager.CreateBoolRow(alterOption.name, alterOption.value, alterOption.callback, alterOption.tooltip);
						num2++;
					}
				}
			}
			if (alter is IAlterOptions<float> alterOptions6)
			{
				if (alterOptions6.options == null)
				{
					continue;
				}
				AlterOption<float>[] options2 = alterOptions6.options;
				foreach (AlterOption<float> alterOption2 in options2)
				{
					if (!alterOption2.hidden)
					{
						elementManager.CreateFloatRow(alterOption2.name, alterOption2.value, alterOption2.callback, alterOption2.constraints, alterOption2.tooltip);
						num2++;
					}
				}
			}
			if (alter is IAlterOptions<Vector3> alterOptions7)
			{
				if (alterOptions7.options == null)
				{
					continue;
				}
				AlterOption<Vector3>[] options3 = alterOptions7.options;
				foreach (AlterOption<Vector3> alterOption3 in options3)
				{
					if (!alterOption3.hidden)
					{
						elementManager.CreateVector3Row(alterOption3.name, alterOption3.value, alterOption3.callback, alterOption3.tooltip);
						num2++;
					}
				}
			}
			if (alter is IAlterOptions<int> alterOptions8)
			{
				if (alterOptions8.options == null)
				{
					continue;
				}
				AlterOption<int>[] options4 = alterOptions8.options;
				foreach (AlterOption<int> alterOption4 in options4)
				{
					if (!alterOption4.hidden)
					{
						Type type = alterOption4.type;
						if (!(type == null) && type.IsEnum)
						{
							elementManager.CreateEnumRow(alterOption4.name, alterOption4.value, alterOption4.callback, type, alterOption4.tooltip);
							num2++;
						}
					}
				}
			}
			if (num2 == 0)
			{
				elementManager.DestroyLastRow();
			}
		}
	}

	public void Close()
	{
		if (SandboxArmDebug.DebugActive)
		{
			Log.Info("Closing Alter Menu");
		}
		shadow.SetActive(value: false);
		menu.SetActive(value: false);
		editedObject = null;
		alterInstance?.EndSession();
		MonoSingleton<CameraController>.Instance.activated = true;
		MonoSingleton<GunControl>.Instance.activated = true;
	}

	private void Update()
	{
		if (editedObject == null && menu.activeSelf)
		{
			Close();
			MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("<color=red>Altered object was destroyed.</color>");
		}
		if (!menu.activeSelf && shadow.activeSelf)
		{
			alterInstance.EndSession();
			shadow.SetActive(value: false);
			editedObject = null;
		}
	}
}
