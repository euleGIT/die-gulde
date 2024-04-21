using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gulde.Client.Model.Groups;
using Gulde.Client.Model.Scenes;
using Newtonsoft.Json;
using Siccity.GLTFUtility;
using TreeEditor;
using UnityEngine;
using static Siccity.GLTFUtility.GLTFAccessor.Sparse;

namespace Gulde.Client
{
    public class SceneLoader : MonoBehaviour
    {
        public static void LoadScene(string basePath, string scenePath)
        {
            var objectNames = new Dictionary<string, string>();

            var objectsPath = Path.Combine(basePath, "objects");
            var files = Directory.GetFiles(objectsPath, "*.glb", SearchOption.AllDirectories);

            foreach (var item in files)
            {
                var key = Path.GetFileNameWithoutExtension(item).ToLower();

                if (objectNames.ContainsKey(key))
                    continue;

                objectNames.Add(key, item);
            }

            scenePath = Path.Combine(basePath, scenePath);
            var sceneAsString = File.ReadAllText(scenePath);

            var gildeScene = JsonConvert.DeserializeObject<GildeScene>(sceneAsString);

            LoadSceneElements(gildeScene, objectNames);
        }

        static void LoadSceneElements(GildeScene gildeScene, Dictionary<string, string> objects)
        {
            var sceneElementToGameObject = new Dictionary<SceneElement, GameObject>();

            var mainParentObject = new GameObject("scene_elements");
            var currentParent = mainParentObject.transform;

            for (var i = 0; i < gildeScene.SceneElements.Length; i++)
            {
                var element = gildeScene.SceneElements[i];

                if (element.OnesCount == 1)
                    currentParent = mainParentObject.transform;

                if (!currentParent)
                {
                    Debug.LogWarning($"The parent of element \"{element.Name}\" was not found.");
                    currentParent = mainParentObject.transform;
                }

                if (element.TransformElement is not null)
                {
                    var gameObject = LoadTransformElement(element, currentParent, objects);
                    sceneElementToGameObject.Add(element, gameObject);
                }
                else if (element.CityElement is not null)
                {
                    var gameObject = LoadCityElement(element.Width, element.Height, element.CityElement, currentParent);
                    sceneElementToGameObject.Add(element, gameObject);
                }
                else
                {
                    var gameObject = new GameObject(element.Name);
                    ApplyTransform(gameObject, null, currentParent);
                    sceneElementToGameObject.Add(element, gameObject);
                }

                if (!sceneElementToGameObject[element])
                    continue;

                if (element.Name == "ordner_BAUKLASSEN_FREI")
                {
                    Debug.Log("OIAJSDOI");
                }

                if (element.SkipLength == 0)
                    currentParent = sceneElementToGameObject[element].transform;
                else if (element.SkipLength >= 2)
                {
                    if (element.Name.EndsWith("MegaCam"))
                    {
                        currentParent = sceneElementToGameObject[element].transform.parent;
                        continue;
                    }

                    var elementSkipCount = Mathf.FloorToInt((element.SkipLength - 2) / 4);
                    currentParent = GetNewParent(sceneElementToGameObject[element].transform, elementSkipCount);
                }
            }

            Debug.Log("Loaded scene elements.");
        }

        static Transform GetNewParent(Transform element, int elementSkipCount)
        {
            var siblingCount = element.parent.childCount;
            var siblingIndex = siblingCount - elementSkipCount;

            if (siblingIndex <= 0)
            {
                elementSkipCount -= siblingCount;
                return GetNewParent(element.parent, elementSkipCount);
            }

            return element.parent;
        }

        static GameObject LoadTransformElement(SceneElement sceneElement, Transform parent, Dictionary<string, string> objects)
        {
            var gameObject = (GameObject)null;
            
            if (sceneElement.TransformElement is DummyElement dummyElement)
            {
                gameObject = new GameObject(sceneElement.Name);
            }
            else if (sceneElement.TransformElement is ObjectElement objectElement)
            {
                if (objectElement.Name is not null && objects.TryGetValue(objectElement.Name.ToLower(), out var gltfPath))
                {
                    gameObject = LoadObject(gltfPath);

                    if (gameObject is null)
                    {
                        return null;
                    }
                    
                    gameObject.name = objectElement.Name;
                }
                else
                {
                    Debug.LogWarning($"Object \"{objectElement.Name}\" not found.");
                }
            }
            
            if (gameObject is null)
                return null;
            
            ApplyTransform(gameObject, sceneElement, parent);
            
            return gameObject;
        }

        static void ApplyTransform(GameObject gameObject, SceneElement sceneElement = null, Transform parentObject = null)
        {
            if (parentObject is not null)
            {
                gameObject.transform.SetParent(parentObject);
            }
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;

            var transformElement = sceneElement?.TransformElement;
            if (transformElement is null)
                return;
            
            var gildeTransform = gameObject.AddComponent<GildeTransform>();
            
            var position = new Vector3(-transformElement.Transform.Position.x, transformElement.Transform.Position.y, -transformElement.Transform.Position.z);
            var rotationRad = new Vector3(transformElement.Transform.Rotation.x, -transformElement.Transform.Rotation.y, transformElement.Transform.Rotation.z);
            // var rotationRad = new Vector3(0, transformElement.Transform.Rotation.y+ transformElement.Transform.Rotation.x, 0);
            var rotationDeg = rotationRad * Mathf.Rad2Deg;
            var hasXZRotation = rotationDeg.x >= 1 || rotationDeg.z >= 1;
            if (hasXZRotation)
                rotationDeg.y *= -1;
            // rotationDeg = Quaternion.Euler(rotationDeg).eulerAngles;

            gildeTransform.position = position;
            gildeTransform.rotation = new Vector3(transformElement.Transform.Rotation.x, transformElement.Transform.Rotation.y, transformElement.Transform.Rotation.z) * Mathf.Rad2Deg;

            // if (parentObject)
            // {
            //     var parentGildeTransform = parentObject.GetComponent<GildeTransform>();
            //
            //     if (parentGildeTransform)
            //     {
            //         rotationDeg += parentGildeTransform.rotation;
            //     }
            // }
            
            gameObject.transform.localPosition = position;
            gameObject.transform.localEulerAngles = rotationDeg;
            gameObject.transform.localScale = Vector3.one;

            if (sceneElement?.Name == "ordner_LICHT_INNEN")
            {
                Debug.Log($"Actual Rotation: {gameObject.transform.eulerAngles}");
                Debug.Log($"Gilde Rotation: {gildeTransform.rotation}");
            }
        }
        
        static GameObject LoadCityElement(int width, int height, CityElement cityElement, Transform parentObject)
        {
            var terrainObject = new GameObject("Terrain");
            terrainObject.transform.SetParent(parentObject.transform);
            var terrain = terrainObject.AddComponent<Terrain>();
            var terrainData = new TerrainData();
            
            var heights = new float[width, height];
            var maxHeight = cityElement.HeightData1.Max();
            
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    heights[x, y] = (float)cityElement.HeightData1[x + y * width] / maxHeight;
                }
            }
            
            terrainData.heightmapResolution = width;
            terrainData.size = new Vector3(width, 100, height);
            terrainData.SetHeights(0, 0, heights);
            terrain.terrainData = terrainData;
            
            return terrainObject;
        }

        static GameObject LoadObject(string path)
        {
            if (path.EndsWith("ub_BLUMEN.glb"))
                return new GameObject("Blumen");

            if (path.EndsWith("ub_KRAUT.glb"))
                return new GameObject("Kraut");

            try
            {
                return Importer.LoadFromFile(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not load object {path}\n{e}");
                return new GameObject($"[Error] {Path.GetFileNameWithoutExtension(path)}");
            }
        }
    }
}
