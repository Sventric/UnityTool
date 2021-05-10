using UnityEngine;

public static class ToolGuiStyles
{
	public static GUIStyle Warning = CreateStyle(GUI.skin.label, TextAnchor.MiddleCenter, Color.red);

	private static GUIStyle CreateStyle(GUIStyle baseStyle = null, TextAnchor alignment = TextAnchor.MiddleLeft, Color? foregroundColor = null)
	{
		GUIStyle style = baseStyle == null ? new GUIStyle() : new GUIStyle(GUI.skin.label);
		
		style.alignment = alignment;

		if (foregroundColor.HasValue)
		{
			style.normal.textColor = foregroundColor.Value;
		}

		return style;
	}
}