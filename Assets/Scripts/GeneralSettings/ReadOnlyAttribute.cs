/*******************************************************************************
* Copyright 2025 INTRIG
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
******************************************************************************/

// Libraries
using UnityEngine; // Unity Engine library to use in PropertyAttribute classes

// Select Unity Editor:
#if UNITY_EDITOR
using UnityEditor;
#endif

// Defines the ReadOnly attribute:
public class ReadOnlyAttribute : PropertyAttribute { }

// Creates a CustomPropertyDrawer for ReadOnly, which is what makes it “opaque” and not editable in the Inspector:
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;  // Disable editing for a moment
        EditorGUI.PropertyField(position, property, label); // Draw the property for ReadOnly
        GUI.enabled = true;   // Enable editing for other variables
    }
}
#endif
