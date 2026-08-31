#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(Widget_Button), true)]
[CanEditMultipleObjects]
public class Widget_Button_Editor : ButtonEditor{
	private SerializedProperty DefaultBorderColor;
	private SerializedProperty HighlightedBorderColor;
	private SerializedProperty DefaultBackgroundOpacity;
	private SerializedProperty HighlightedBackgroundOpacity;

	protected override void OnEnable(){
		base.OnEnable();

		DefaultBorderColor = serializedObject.FindProperty("DefaultBorderColor");
		HighlightedBorderColor = serializedObject.FindProperty("HighlightedBorderColor");
		DefaultBackgroundOpacity = serializedObject.FindProperty("DefaultBackgroundOpacity");
		HighlightedBackgroundOpacity = serializedObject.FindProperty("HighlightedBackgroundOpacity");
	}

	public override void OnInspectorGUI(){
		base.OnInspectorGUI();

		serializedObject.Update();

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Border Color", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(DefaultBorderColor);
		EditorGUILayout.PropertyField(HighlightedBorderColor);

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Background Opacity", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(DefaultBackgroundOpacity);
		EditorGUILayout.PropertyField(HighlightedBackgroundOpacity);

		serializedObject.ApplyModifiedProperties();
	}
}
#endif
