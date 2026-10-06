using UnityEngine;
using UnityEngine.UI;

public class LoadingBar : MonoBehaviour
{
	private Material barMaterial;

	private Image barImage;

	private float loadTime;

	private void OnEnable()
	{
		loadTime = 0f;
		barImage = GetComponent<Image>();
		barMaterial = barImage.materialForRendering;
		barMaterial.SetFloat("_LoadTime", 0f);
		barImage.SetMaterialDirty();
	}

	private void Update()
	{
		loadTime += Time.deltaTime;
		barMaterial.SetFloat("_LoadTime", loadTime);
		barImage.SetMaterialDirty();
	}
}
