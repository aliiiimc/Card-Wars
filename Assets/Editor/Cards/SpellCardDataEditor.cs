using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpellCardData))]
public class SpellCardDataEditor : Editor
{
    private SerializedProperty cardNameProperty;
    private SerializedProperty costProperty;
    private SerializedProperty handDeckSpriteProperty;
    private SerializedProperty descriptionProperty;
    private SerializedProperty effectTypeProperty;
    private SerializedProperty effectPowerProperty;
    private SerializedProperty effectDurationTurnsProperty;
    private SerializedProperty spellMovementCapacityProperty;

    private void OnEnable()
    {
        cardNameProperty = serializedObject.FindProperty("cardName");
        costProperty = serializedObject.FindProperty("cost");
        handDeckSpriteProperty = serializedObject.FindProperty("handDeckSprite");
        descriptionProperty = serializedObject.FindProperty("description");
        effectTypeProperty = serializedObject.FindProperty("effectType");
        effectPowerProperty = serializedObject.FindProperty("effectPower");
        effectDurationTurnsProperty = serializedObject.FindProperty("effectDurationTurns");
        spellMovementCapacityProperty = serializedObject.FindProperty("spellMovementCapacity");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(cardNameProperty);
        EditorGUILayout.PropertyField(costProperty);
        EditorGUILayout.PropertyField(handDeckSpriteProperty, new GUIContent("Sprite"));
        EditorGUILayout.PropertyField(descriptionProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spell", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(effectTypeProperty);
        EditorGUILayout.PropertyField(effectPowerProperty);
        EditorGUILayout.PropertyField(effectDurationTurnsProperty);
        EditorGUILayout.PropertyField(spellMovementCapacityProperty);

        serializedObject.ApplyModifiedProperties();
    }
}