using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorBlindGet : MonoBehaviour
{
	public HudColorType hct;

	private Image img;

	private Text txt;

	private Light lit;

	private SpriteRenderer sr;

	private TMP_Text txt2;

	private ParticleSystem ps;

	private bool gotTarget;

	public bool variationColor;

	public int variationNumber;

	public bool customColorRenderer;

	private Renderer rend;

	private MaterialPropertyBlock block;

	private void Start()
	{
		UpdateColor();
	}

	private void OnEnable()
	{
		UpdateColor();
	}

	public void UpdateColor()
	{
		if (!gotTarget)
		{
			GetTarget();
		}
		Color color = (variationColor ? MonoSingleton<ColorBlindSettings>.Instance.variationColors[variationNumber] : MonoSingleton<ColorBlindSettings>.Instance.GetHudColor(hct));
		if ((bool)rend)
		{
			rend.GetPropertyBlock(block);
			block.SetColor("_CustomColor1", color);
			rend.SetPropertyBlock(block);
			return;
		}
		if ((bool)img)
		{
			img.color = color;
		}
		if ((bool)txt)
		{
			txt.color = color;
		}
		if ((bool)txt2)
		{
			txt2.color = color;
		}
		if ((bool)lit)
		{
			lit.color = color;
		}
		if ((bool)sr)
		{
			sr.color = color;
		}
		if ((bool)ps)
		{
			ParticleSystem.MainModule main = ps.main;
			main.startColor = color;
		}
	}

	private void GetTarget()
	{
		gotTarget = true;
		if (customColorRenderer)
		{
			rend = GetComponent<Renderer>();
			block = new MaterialPropertyBlock();
		}
		img = GetComponent<Image>();
		txt = GetComponent<Text>();
		txt2 = GetComponent<TMP_Text>();
		lit = GetComponent<Light>();
		sr = GetComponent<SpriteRenderer>();
		ps = GetComponent<ParticleSystem>();
	}
}
