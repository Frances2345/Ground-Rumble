// TEMPORARY WORKAROUND
// The built-in com.unity.render-pipelines.core package shipped with Unity 6000.6.0f1
// is missing this source file, which makes the editor assembly fail to compile with
// CS0246. This minimal implementation satisfies the API used by
// ReadonlyMaterialConverter.cs so the project can compile.
// It does not perform any material reassignment. Reinstall/repair Unity 6.6 for a
// proper fix, then remove this file.
using System;
using System.Text;

namespace UnityEditor.Rendering.Converter
{
    internal class MaterialReferenceChanger : IDisposable
    {
        public MaterialReferenceChanger(ReadonlyMaterialMap mappings)
        {
        }

        public void Dispose()
        {
        }

        public bool ReassignUnityObjectMaterials(UnityEngine.Object obj, StringBuilder message)
        {
            return true;
        }
    }
}
