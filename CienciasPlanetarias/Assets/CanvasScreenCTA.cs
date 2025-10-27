using UnityEngine;

public class CanvasScreenCTA : CanvasScreen
{
	// Chame este método no botão PT
	public void SetLanguagePT()
	{
		if (LocalizationManager.instance != null)
		{
			LocalizationManager.instance.SetLanguage("pt");
		}
	}

	// Chame este método no botão EN
	public void SetLanguageEN()
	{
		if (LocalizationManager.instance != null)
		{
			LocalizationManager.instance.SetLanguage("en");
		}
	}
}
