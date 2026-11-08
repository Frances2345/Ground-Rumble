using UnityEditor;
using UnityEngine;

namespace GroundRumble.EditorTools
{
    [CustomEditor(typeof(BuildingGenerator))]
    public class BuildingGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10f);

            BuildingGenerator generator = (BuildingGenerator)target;

            if (GUILayout.Button("Generar edificio de 30 pisos", GUILayout.Height(30f)))
            {
                generator.Generate();
            }

            if (GUILayout.Button("Limpiar edificio generado", GUILayout.Height(24f)))
            {
                generator.ClearGenerated();
            }
        }
    }
}
