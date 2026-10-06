using TMPro;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class CutsceneSkipText : MonoSingleton<CutsceneSkipText>
{
	private TMP_Text txt;

	private void Start()
	{
		txt = GetComponent<TMP_Text>();
		Hide();
	}

	public void Show()
	{
		txt.enabled = true;
	}

	public void Hide()
	{
		txt.enabled = false;
	}
}
