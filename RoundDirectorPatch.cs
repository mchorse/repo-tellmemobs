using System;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TellMeMobs;

[HarmonyPatch(typeof(RoundDirector))]
public static class RoundDirectorPatch
{
	public static GameObject? textObject;
	public static TextMeshProUGUI? textMesh;

	public static string FormatMessageWithLineBreaks(string message, int lineLength = 32)
	{
		int lastBreak = 0;
		int searchStart = 0;
		
		StringBuilder sb = new StringBuilder();

		while (searchStart < message.Length)
		{
			int nextBreak = lastBreak + lineLength;
			
			if (nextBreak >= message.Length)
			{
				sb.Append(message.Substring(lastBreak));
				
				break;
			}
			
			int breakPos = message.LastIndexOf(", ", nextBreak, nextBreak - lastBreak, StringComparison.Ordinal);
			
			breakPos = breakPos == -1 || breakPos < lastBreak ? nextBreak : breakPos + 2;

			sb.Append(message.Substring(lastBreak, breakPos - lastBreak));
			sb.Append('\n');
			
			lastBreak = breakPos;
			searchStart = lastBreak;
		}

		return sb.ToString();
	}

	[HarmonyPostfix, HarmonyPatch(nameof(RoundDirector.Update))]
	public static void UpdateUI()
	{
		if (!SemiFunc.RunIsLevel())
		{
			return;
		}
		
		if (textObject == null)
		{
			GameObject hud = GameObject.Find("Game Hud");
			GameObject tax = GameObject.Find("Tax Haul");
			bool hudAndTaxPresent = hud == null || tax == null;
			
			if (!hudAndTaxPresent)
			{
				textObject = new GameObject();
				textObject.SetActive(false);
				textObject.name = "Mob Info";
				textObject.AddComponent<TextMeshProUGUI>();
				textObject.transform.SetParent(hud?.transform, false);
				
				var sizeFitter = textObject.AddComponent<ContentSizeFitter>();
				
				sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
				sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

				textMesh = textObject.GetComponent<TextMeshProUGUI>();
				textMesh.font = tax?.GetComponent<TMP_Text>().font;
				textMesh.color = new Vector4(1f, 1f, 1f, 1f);
				textMesh.fontSize = 24f;
				textMesh.enableWordWrapping = false;
				textMesh.alignment = TextAlignmentOptions.BaselineRight;
				textMesh.horizontalAlignment = HorizontalAlignmentOptions.Right;
				textMesh.verticalAlignment = VerticalAlignmentOptions.Bottom;

				RectTransform component = textObject.GetComponent<RectTransform>();
				
				component.pivot = new Vector2(1f, 0f);
				component.anchoredPosition = new Vector2(0f, 0f);
				component.anchorMin = new Vector2(0f, 0f);
				component.anchorMax = new Vector2(1f, 0f);
			}
		}
		else if (textMesh != null)
		{
			string message = TellMeMobs.GetMobInformation();
			bool hasMessage = message.Length > 0;
			MobLabelVisibility visibility = TellMeMobs.LabelVisibility.Value;
			bool mapOpen = SemiFunc.InputHold(InputKey.Map) || Traverse.Create(MapToolController.instance).Field("mapToggled").GetValue<bool>();
			bool showLabel = hasMessage && (visibility == MobLabelVisibility.Visible || (visibility == MobLabelVisibility.MapVisible && mapOpen));

			textObject.SetActive(showLabel);
			
			if (hasMessage)
			{
				int step = Mathf.Clamp((message.Length - 1) / 48, 0, 3);
				float[] sizes = {24f, 18f, 14f, 11f};
				float fontSize = sizes[step];
				string finalMsg = FormatMessageWithLineBreaks(message, 48);

				textMesh.SetText(finalMsg, true);
				textMesh.fontSize = fontSize;
				textMesh.lineSpacing = -fontSize * 1.5f;
			}
		}
	}
}