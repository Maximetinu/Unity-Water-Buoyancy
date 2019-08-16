using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(WaterVolume))]
public class WaterVolumeEditor : Editor
{
    private const float BOX_COLLIDER_HEIGHT = 5f;

    private WaterVolume waterVolumeTarget;
    private SerializedProperty rows;
    private SerializedProperty columns;
    private SerializedProperty quadSegmentSize;

    protected virtual void OnEnable()
    {
        this.waterVolumeTarget = (WaterVolume)this.target;

        this.rows = this.serializedObject.FindProperty("rows");
        this.columns = this.serializedObject.FindProperty("columns");
        this.quadSegmentSize = this.serializedObject.FindProperty("quadSegmentSize");

        Undo.undoRedoPerformed += this.OnUndoRedoPerformed;

        waterVolumeTarget.GetComponent<MeshFilter>().hideFlags = HideFlags.NotEditable;
        waterVolumeTarget.GetComponent<BoxCollider>().hideFlags = HideFlags.NotEditable;
        waterVolumeTarget.GetComponent<BoxCollider>().isTrigger = true;
        waterVolumeTarget.GetComponent<BoxCollider>().enabled = waterVolumeTarget.enabled;
    }

    protected virtual void OnDisable()
    {
        Undo.undoRedoPerformed -= this.OnUndoRedoPerformed;
    }

    public override void OnInspectorGUI()
    {
        this.serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        if (waterVolumeTarget.GetComponent<MeshFilter>().sharedMesh == null && !PrefabUtility.IsPartOfPrefabAsset(waterVolumeTarget))
        {
            this.UpdateMesh(this.rows.intValue, this.columns.intValue, this.quadSegmentSize.floatValue);
            this.UpdateBoxCollider(this.rows.intValue, this.columns.intValue, this.quadSegmentSize.floatValue);
        }

        EditorGUILayout.PropertyField(this.rows);
        EditorGUILayout.PropertyField(this.columns);
        EditorGUILayout.PropertyField(this.quadSegmentSize);
        if (EditorGUI.EndChangeCheck())
        {
            this.rows.intValue = Mathf.Max(1, this.rows.intValue);
            this.columns.intValue = Mathf.Max(1, this.columns.intValue);
            this.quadSegmentSize.floatValue = Mathf.Max(0f, this.quadSegmentSize.floatValue);

            this.UpdateMesh(this.rows.intValue, this.columns.intValue, this.quadSegmentSize.floatValue);
            this.UpdateBoxCollider(this.rows.intValue, this.columns.intValue, this.quadSegmentSize.floatValue);
        }

        this.serializedObject.ApplyModifiedProperties();
    }

    private void UpdateMesh(int rows, int columns, float quadSegmentSize)
    {
        if (Application.isPlaying)
        {
            return;
        }

        MeshFilter meshFilter = this.waterVolumeTarget.GetComponent<MeshFilter>();

        Mesh newMesh = WaterMeshGenerator.GenerateMesh(rows, columns, quadSegmentSize);
        newMesh.name = "Water Mesh Instance";

        meshFilter.sharedMesh = newMesh;

        EditorUtility.SetDirty(meshFilter);
    }

    private void UpdateBoxCollider(int rows, int columns, float quadSegmentSize)
    {
        var boxCollider = this.waterVolumeTarget.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Vector3 size = new Vector3(columns * quadSegmentSize, BOX_COLLIDER_HEIGHT, rows * quadSegmentSize);
            boxCollider.size = size;

            Vector3 center = size / 2f;
            center.y *= -1f;
            boxCollider.center = center;

            EditorUtility.SetDirty(boxCollider);
        }
    }

    private void OnUndoRedoPerformed()
    {
        this.UpdateMesh(this.waterVolumeTarget.Rows, this.waterVolumeTarget.Columns, this.waterVolumeTarget.QuadSegmentSize);
        this.UpdateBoxCollider(this.waterVolumeTarget.Rows, this.waterVolumeTarget.Columns, this.waterVolumeTarget.QuadSegmentSize);
    }
}
