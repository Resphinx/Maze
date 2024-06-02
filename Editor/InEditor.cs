using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Resphinx.Maze.Counting))]
public class CountingEditor : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
    //    property.serializedObject.Update();
        float total = 0, w = 0;
        bool asIs = property.FindPropertyRelative("asIs").boolValue;
        EditorGUI.BeginProperty(position, label, property);
        var asIsRect = new Rect(position.x, position.y, w = 15, position.height);
     //   position.x += w + 5;
     //   position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;


        //     var asIsLabel = new Rect(position.x, position.y, w = 30, position.height);

        var byLabel = new Rect(position.x + (total += w + 5), position.y, w = 40, position.height);
        var byRect = new Rect(position.x + (total += w + 5), position.y, w = 15, position.height);
        var countLabel = new Rect(position.x + (total += w + 5), position.y, w = 35, position.height);
        var countRect = new Rect(position.x + (total += w + 5), position.y, w = 25, position.height);


        // EditorGUI.LabelField(asIsLabel, "asIs");
        EditorGUI.PropertyField(asIsRect, property.FindPropertyRelative("asIs"), GUIContent.none);
        if (!asIs)
        {
            EditorGUI.LabelField(byLabel, "Fixed:");
            EditorGUI.PropertyField(byRect, property.FindPropertyRelative("byCount"), GUIContent.none);
            EditorGUI.LabelField(countLabel, "Pool:");
            EditorGUI.PropertyField(countRect, property.FindPropertyRelative("pool"), GUIContent.none);
        }
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
        property.serializedObject.ApplyModifiedProperties();
    }
}
[CustomPropertyDrawer(typeof(Resphinx.Maze.Placement))]
public class PlacementEditor : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        float total = 0, w = 0;
      //  property.serializedObject.Update();
        EditorGUI.BeginProperty(position, label, property);
        position.x += w + 5;
     //   position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;


        //     var asIsLabel = new Rect(position.x, position.y, w = 30, position.height);

        var xL = new Rect(position.x, position.y, w = 15, position.height);
        var xR = new Rect(position.x + (total += w + 5), position.y, w = 25, position.height);
        var yL = new Rect(position.x + (total += w + 5), position.y, w = 15, position.height);
        var yR = new Rect(position.x + (total += w + 5), position.y, w = 25, position.height);
        var zL = new Rect(position.x + (total += w + 5), position.y, w = 15, position.height);
        var zR = new Rect(position.x + (total += w + 5), position.y, w = 25, position.height);
        var dL = new Rect(position.x + (total += w + 5), position.y, w = 15, position.height);
        var dR = new Rect(position.x + (total += w + 5), position.y, w = 25, position.height);


        EditorGUI.LabelField(xL, "x");
        EditorGUI.PropertyField(xR, property.FindPropertyRelative("x"), GUIContent.none);
        EditorGUI.LabelField(yL, "y");
        EditorGUI.PropertyField(yR, property.FindPropertyRelative("y"), GUIContent.none);
        EditorGUI.LabelField(zL, "z");
        EditorGUI.PropertyField(zR, property.FindPropertyRelative("z"), GUIContent.none);
        EditorGUI.LabelField(dL, "d");
        EditorGUI.PropertyField(dR, property.FindPropertyRelative("d"), GUIContent.none);
      
        EditorGUI.indentLevel = indent;
     
        EditorGUI.EndProperty();
        property.serializedObject.ApplyModifiedProperties();
    }
}
[CustomEditor(typeof(Resphinx.Maze.PrefabSettings)), CanEditMultipleObjects]
public class PrefabGlobalEditor : Editor
{
    SerializedProperty type;
    SerializedProperty alwaysVisible;
    SerializedProperty centerType;
    SerializedProperty byCount;
    SerializedProperty pool;
    SerializedProperty edge;
    SerializedProperty corner;
    SerializedProperty length;
    SerializedProperty height;
    SerializedProperty width;
    SerializedProperty positions;
    SerializedProperty wallType;
    SerializedProperty opening;
    SerializedProperty mirrored;
    SerializedProperty id;
    SerializedProperty rotatable;
    SerializedProperty side;
    SerializedProperty adjacentTo;
    SerializedProperty switchSides;
    void OnEnable()
    {
        type = serializedObject.FindProperty("type");
        alwaysVisible = serializedObject.FindProperty("alwaysVisible");
        centerType = serializedObject.FindProperty("centerType");
        byCount = serializedObject.FindProperty("byCount");
        pool = serializedObject.FindProperty("pool");
        edge = serializedObject.FindProperty("edge");
        corner = serializedObject.FindProperty("corner");
        length = serializedObject.FindProperty("length");
        height = serializedObject.FindProperty("height");
        width = serializedObject.FindProperty("width");
        positions = serializedObject.FindProperty("positions");
          wallType = serializedObject.FindProperty("wallType");
        opening = serializedObject.FindProperty("opening");
        mirrored = serializedObject.FindProperty("mirrored");
        switchSides = serializedObject.FindProperty("switchSides");
        id = serializedObject.FindProperty("id");
        rotatable = serializedObject.FindProperty("rotatable");
        side = serializedObject.FindProperty("side");
        adjacentTo = serializedObject.FindProperty("adjacentTo");

    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Resphinx.Maze.PrefabSettings m = (Resphinx.Maze.PrefabSettings)target;

        EditorGUILayout.PropertyField(type);
        if (m.type == Resphinx.Maze.ModelType.Wall)
            EditorGUILayout.PropertyField(wallType);
        EditorGUILayout.PropertyField(alwaysVisible);
        EditorGUILayout.PropertyField(byCount);
        EditorGUILayout.PropertyField(pool);
        EditorGUILayout.PropertyField(edge);
        EditorGUILayout.PropertyField(corner);
        EditorGUILayout.PropertyField(side);
        if (m.type == Resphinx.Maze.ModelType.Wall)
        {
            EditorGUILayout.PropertyField(centerType);
            EditorGUILayout.PropertyField(rotatable);
            EditorGUILayout.PropertyField(switchSides);

            if (m.wallType == Resphinx.Maze.WallType.Open)
            {
                EditorGUILayout.PropertyField(opening);
                EditorGUILayout.PropertyField(mirrored);
            }

        }
        else if (m.type == Resphinx.Maze.ModelType.Floor)
        {
            m.switchSides = false;
            EditorGUILayout.PropertyField(length);
            if (m.length < 1) m.length = 1;
            if (m.length > 1)
            {
                EditorGUILayout.PropertyField(width);
                EditorGUILayout.PropertyField(height);
                EditorGUILayout.PropertyField(rotatable);
                EditorGUILayout.PropertyField(positions);
               }
        }
        else if (m.type == Resphinx.Maze.ModelType.Item)
        {
            EditorGUILayout.PropertyField(centerType);
            EditorGUILayout.PropertyField(id);
            EditorGUILayout.PropertyField(rotatable);
            EditorGUILayout.PropertyField(adjacentTo);

        }
        serializedObject.ApplyModifiedProperties();
    }
}