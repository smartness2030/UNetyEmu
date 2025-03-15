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
using UnityEngine; // Unity Engine library to use in MonoBehaviour classes

// Class to add a read component about the depth camera's features, according to the data in the setupObjects.json file:
public class GetDepthCameraFeatures : MonoBehaviour
{
    // Depth Camera Features:
    [Header("Depth Camera Features")]
    [ReadOnly] public string scriptName;
    [ReadOnly] public float nearClipPlane;
    [ReadOnly] public float farClipPlane;
    [ReadOnly] public float fieldOfView;

    // Render Texture Features:
    [Header("Render Texture Features")]
    [ReadOnly] public int pixelWidth;
    [ReadOnly] public int pixelHeight;
}
