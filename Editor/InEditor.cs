using UnityEditor;
[CustomEditor(typeof(Resphinx.Maze.PrefabSettings)), CanEditMultipleObjects]
public class ModelCountEditor : Editor
{
    SerializedProperty type;
    SerializedProperty alwaysVisible;
    SerializedProperty centerType;
    SerializedProperty byCount;
    SerializedProperty count;
    SerializedProperty edge;
    SerializedProperty corner;
    SerializedProperty length;
    SerializedProperty height;
    SerializedProperty width;
    SerializedProperty positions;
    SerializedProperty directions;
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
        count = serializedObject.FindProperty("count");
        edge = serializedObject.FindProperty("edge");
        corner = serializedObject.FindProperty("corner");
        length = serializedObject.FindProperty("length");
        height = serializedObject.FindProperty("height");
        width = serializedObject.FindProperty("width");
        positions = serializedObject.FindProperty("positions");
        directions = serializedObject.FindProperty("directions");
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
        EditorGUILayout.PropertyField(count);
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
                EditorGUILayout.PropertyField(directions);
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