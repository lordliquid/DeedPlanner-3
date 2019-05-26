﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;

namespace Warlander.Deedplanner.Logic
{
    public class HeightUpdater : MonoBehaviour
    {

        private readonly Color neutralColor = Color.white;
        private readonly Color hoveredColor = new Color(0.7f, 0.7f, 0, 1);

        private HeightmapHandle hoveredHandle = null;

        private bool validDragging = false;
        private bool dragging = false;
        private Vector2 dragStartPos;
        private Vector2 dragEndPos;

        public void OnEnable()
        {
            LayoutManager.Instance.TileSelectionMode = TileSelectionMode.Nothing;
        }

        private void Update()
        {
            RaycastHit raycast = LayoutManager.Instance.CurrentCamera.CurrentRaycast;

            Ground ground = raycast.transform ? raycast.transform.GetComponent<Ground>() : null;
            HeightmapHandle heightmapHandle =  raycast.transform ? raycast.transform.GetComponent<HeightmapHandle>() : null;

            if (!heightmapHandle && hoveredHandle)
            {
                hoveredHandle.Color = neutralColor;
                hoveredHandle = null;
            }
            else if (heightmapHandle && heightmapHandle != hoveredHandle)
            {
                if (hoveredHandle)
                {
                    hoveredHandle.Color = neutralColor;
                }
                heightmapHandle.Color = hoveredColor;
                hoveredHandle = heightmapHandle;
            }

            if (!raycast.transform)
            {
                return;
            }

            if (Input.GetMouseButton(0))
            {
                if (!dragging)
                {
                    dragStartPos = LayoutManager.Instance.CurrentCamera.MousePosition;
                    dragEndPos = dragStartPos;
                    dragging = true;
                }
                else if (dragging)
                {
                    dragEndPos = LayoutManager.Instance.CurrentCamera.MousePosition;
                }

                if (Vector2.Distance(dragStartPos, dragEndPos) > 5)
                {
                    validDragging = true;
                    LayoutManager.Instance.CurrentCamera.RenderSelectionBox = true;
                }
                Vector2 difference = dragEndPos - dragStartPos;
                float clampedDifferenceX = Mathf.Clamp(-difference.x, 0, float.MaxValue);
                float clampedDifferenceY = Mathf.Clamp(-difference.y, 0, float.MaxValue);
                Vector2 clampedDifference = new Vector2(clampedDifferenceX, clampedDifferenceY);

                Vector2 selectionStart = dragStartPos - clampedDifference;
                Vector2 selectionEnd = dragEndPos - dragStartPos + clampedDifference * 2;

                LayoutManager.Instance.CurrentCamera.SelectionBoxPosition = selectionStart;
                LayoutManager.Instance.CurrentCamera.SelectionBoxSize = selectionEnd;

                Vector2 viewportStart = selectionStart / LayoutManager.Instance.CurrentCamera.Screen.GetComponent<RectTransform>().sizeDelta;
                Vector2 viewportEnd = selectionEnd / LayoutManager.Instance.CurrentCamera.Screen.GetComponent<RectTransform>().sizeDelta;
                Rect viewportRect = new Rect(viewportStart, viewportEnd);

                Camera checkedCamera = LayoutManager.Instance.CurrentCamera.AttachedCamera;

                for (int i = 0; i <= GameManager.Instance.Map.Width; i++)
                {
                    for (int i2 = 0; i2 < GameManager.Instance.Map.Height; i2++)
                    {
                        float height = GameManager.Instance.Map[i, i2].GetHeightForFloor(LayoutManager.Instance.CurrentCamera.Floor) * 0.1f;
                        Vector2 viewportLocation = checkedCamera.WorldToViewportPoint(new Vector3(i * 4, height, i2 * 4));
                        if (viewportRect.Contains(viewportLocation))
                        {
                            GameManager.Instance.Map.SurfaceGridMesh.GetHandle(i, i2).Color = hoveredColor;
                        }
                        else
                        {
                            GameManager.Instance.Map.SurfaceGridMesh.GetHandle(i, i2).Color = neutralColor;
                        }
                    }
                }
            }
            else
            {
                dragging = false;
                validDragging = false;
                LayoutManager.Instance.CurrentCamera.RenderSelectionBox = false;
            }

            if (heightmapHandle)
            {
                
            }
            else if (ground)
            {

            }
        }

    }
}