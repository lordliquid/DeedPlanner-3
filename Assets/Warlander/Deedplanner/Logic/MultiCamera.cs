﻿using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityStandardAssets.Water;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Logic
{
    [RequireComponent(typeof(Camera))]
    public class MultiCamera : MonoBehaviour
    {
        private Transform parentTransform;
        public Camera AttachedCamera { get; private set; }
        public Vector2 MousePosition { get; private set; }
        
        [SerializeField] private int screenId = 0;
        [SerializeField] private GameObject screen = null;
        [SerializeField] private CameraMode cameraMode = CameraMode.Top;
        [SerializeField] private int floor = 0;

        [SerializeField] private Water ultraQualityWater = null;
        [SerializeField] private GameObject highQualityWater = null;
        [SerializeField] private GameObject simpleQualityWater = null;

        [SerializeField] private RectTransform selectionBox = null;
        [SerializeField] private Projector attachedProjector = null;

        [SerializeField] private Color pickerColor = new Color(1f, 1f, 0, 0.3f);

        private Vector3 fppPosition = new Vector3(-3, 4, -3);
        private Vector3 fppRotation = new Vector3(15, 45, 0);
        private const float WurmianHeight = 1.4f;

        private Vector2 topPosition;
        private float topScale = 40;

        private Vector2 isoPosition;
        private float isoScale = 40;

        public bool MouseOver { get; private set; } = false;

        public RaycastHit CurrentRaycast { get; private set; }

        public CameraMode CameraMode {
            get => cameraMode;
            set {
                cameraMode = value;
                UpdateState();
            }
        }

        public int Floor {
            get => floor;
            set {
                floor = value;
                UpdateState();
            }
        }

        public bool RenderEntireMap => CameraMode == CameraMode.Perspective || CameraMode == CameraMode.Wurmian;

        public GameObject Screen => screen;

        public bool RenderSelectionBox
        {
            get => selectionBox.gameObject.activeSelf;
            set
            {
                if (selectionBox)
                {
                    selectionBox.gameObject.SetActive(value);
                }
            }
        }

        public Vector2 SelectionBoxPosition {
            get => selectionBox.anchoredPosition;
            set => selectionBox.anchoredPosition = value;
        }

        public Vector2 SelectionBoxSize {
            get => selectionBox.sizeDelta;
            set => selectionBox.sizeDelta = value;
        }

        private void Start()
        {
            parentTransform = transform.parent;
            AttachedCamera = GetComponent<Camera>();

            MouseEventCatcher eventCatcher = screen.GetComponent<MouseEventCatcher>();

            eventCatcher.OnDragEvent.AddListener(data =>
            {
                if (data.button != PointerEventData.InputButton.Middle)
                {
                    return;
                }
                
                if (CameraMode == CameraMode.Perspective || CameraMode == CameraMode.Wurmian)
                {
                    fppRotation += new Vector3(-data.delta.y * Properties.Instance.FppMouseSensitivity, data.delta.x * Properties.Instance.FppMouseSensitivity, 0);
                    fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);
                }
                else if (CameraMode == CameraMode.Top)
                {
                    topPosition += new Vector2(-data.delta.x * Properties.Instance.TopMouseSensitivity, -data.delta.y * Properties.Instance.TopMouseSensitivity);
                }
                else if (CameraMode == CameraMode.Isometric)
                {
                    isoPosition += new Vector2(-data.delta.x * Properties.Instance.IsoMouseSensitivity, -data.delta.y * Properties.Instance.IsoMouseSensitivity);
                }
            });

            eventCatcher.OnBeginDragEvent.AddListener(data =>
            {
                if (data.button == PointerEventData.InputButton.Middle)
                {
                    Cursor.visible = false;
                }
            });

            eventCatcher.OnEndDragEvent.AddListener(data =>
            {
                if (data.button == PointerEventData.InputButton.Middle)
                {
                    Cursor.visible = true;
                }
            });

            eventCatcher.OnPointerEnterEvent.AddListener(data =>
            {
                MouseOver = true;
            });

            eventCatcher.OnPointerExitEvent.AddListener(data =>
            {
                MouseOver = false;
            });

            CameraMode = cameraMode;

            Properties.Instance.Saved += ValidateState;
            ValidateState();
        }

        private void ValidateState()
        {
            Gui.WaterQuality waterQuality = Properties.Instance.WaterQuality;
            if (waterQuality != Gui.WaterQuality.Ultra)
            {
                ultraQualityWater.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            Map map = GameManager.Instance.Map;

            GameObject focusedObject = EventSystem.current.currentSelectedGameObject;
            bool shouldUpdateCameras = !focusedObject;

            if (shouldUpdateCameras)
            {
                if (CameraMode == CameraMode.Perspective || CameraMode == CameraMode.Wurmian)
                {
                    UpdatePerspectiveCamera(map);
                }
                else if (CameraMode == CameraMode.Top)
                {
                    UpdateTopCamera(map);
                }
                else if (CameraMode == CameraMode.Isometric)
                {
                    UpdateIsometricCamera(map);
                }
            }

            UpdateState();
        }

        private void OnPreCull()
        {
            PrepareWater();
            PrepareMapState();
            UpdateRaycast();
            PrepareProjector();
        }

        private void UpdateRaycast()
        {
            CurrentRaycast = default;
            
            if (MouseOver)
            {
                Ray ray = CreateMouseRay();
                RaycastHit raycastHit;
                int mask = LayerMasks.GetMaskForTab(LayoutManager.Instance.CurrentTab);
                bool hit = Physics.Raycast(ray, out raycastHit, 20000, mask);
                StringBuilder tooltipBuild = new StringBuilder();
                
                if (hit && Cursor.visible)
                {
                    CurrentRaycast = raycastHit;

                    bool isHeightEditing = LayoutManager.Instance.CurrentTab == Tab.Height;
                    
                    GameObject hitObject = raycastHit.transform.gameObject;
                    TileEntity tileEntity = hitObject.GetComponent<TileEntity>();
                    GroundMesh groundMesh = hitObject.GetComponent<GroundMesh>();
                    OverlayMesh overlayMesh = hitObject.GetComponent<OverlayMesh>();
                    HeightmapHandle heightmapHandle = GameManager.Instance.Map.SurfaceGridMesh.RaycastHandles();
                    
                    if (tileEntity)
                    {
                        tooltipBuild.Append(tileEntity.ToString());
                    }
                    else if (groundMesh)
                    {
                        int x = Mathf.FloorToInt(raycastHit.point.x / 4f);
                        int y = Mathf.FloorToInt(raycastHit.point.z / 4f);
                        tooltipBuild.Append("X: " + x + " Y: " + y).AppendLine();
                        
                        if (isHeightEditing)
                        {
                            Map map = GameManager.Instance.Map;
                            Vector3 raycastPoint = raycastHit.point;
                            Vector2Int tileCoords = new Vector2Int(Mathf.FloorToInt(raycastPoint.x / 4), Mathf.FloorToInt(raycastPoint.z / 4));
                            int clampedX = Mathf.Clamp(tileCoords.x, 0, map.Width);
                            int clampedY = Mathf.Clamp(tileCoords.y, 0, map.Height);
                            tileCoords = new Vector2Int(clampedX, clampedY);

                            int h00 = map[tileCoords.x, tileCoords.y].GetHeightForFloor(floor);
                            int h10 = map[tileCoords.x + 1, tileCoords.y].GetHeightForFloor(floor);
                            int h01 = map[tileCoords.x, tileCoords.y + 1].GetHeightForFloor(floor);
                            int h11 = map[tileCoords.x + 1, tileCoords.y + 1].GetHeightForFloor(floor);
                            int h00Digits = StringUtils.DigitsStringCount(h00);
                            int h10Digits = StringUtils.DigitsStringCount(h10);
                            int h01Digits = StringUtils.DigitsStringCount(h01);
                            int h11Digits = StringUtils.DigitsStringCount(h11);
                            int maxDigits = Mathf.Max(h00Digits, h10Digits, h01Digits, h11Digits);
                            
                            tooltipBuild.Append("<mspace=0.5em>");
                            tooltipBuild.Append(StringUtils.PaddedNumberString(h01, maxDigits)).Append("   ").Append(StringUtils.PaddedNumberString(h11, maxDigits)).AppendLine();
                            tooltipBuild.AppendLine();
                            tooltipBuild.Append(StringUtils.PaddedNumberString(h00, maxDigits)).Append("   ").Append(StringUtils.PaddedNumberString(h10, maxDigits)).Append("</mspace>");
                        }
                        else
                        {
                            tooltipBuild.Append(GameManager.Instance.Map[x, y].Ground.Data.Name);
                        }
                    }
                    else if (overlayMesh)
                    {
                        int x = Mathf.FloorToInt(raycastHit.point.x / 4f);
                        int y = Mathf.FloorToInt(raycastHit.point.z / 4f);
                        tooltipBuild.Append("X: " + x + " Y: " + y);
                    }
                    else if (heightmapHandle != null)
                    {
                        tooltipBuild.Append(heightmapHandle.ToRichString());
                    }
                }

                LayoutManager.Instance.TooltipText = tooltipBuild.ToString();
            }
        }

        public Ray CreateMouseRay()
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(screen.GetComponent<RectTransform>(), Input.mousePosition, null, out local);
            MousePosition = local + (screen.GetComponent<RectTransform>().sizeDelta / 2);
            local /= screen.GetComponent<RectTransform>().sizeDelta;
            local += new Vector2(0.5f, 0.5f);
            Ray ray = AttachedCamera.ViewportPointToRay(local);
            return ray;
        }

        private void PrepareWater()
        {
            Vector3 cameraPosition = AttachedCamera.transform.position;
            Tab tab = LayoutManager.Instance.CurrentTab;
            bool forceSurfaceEditing = tab == Tab.Ground || tab == Tab.Height;
            int editingFloor = forceSurfaceEditing ? 0 : Floor;
            bool renderWater = RenderEntireMap || editingFloor == 0 || editingFloor == -1;
            if (Properties.Instance.WaterQuality == Gui.WaterQuality.Ultra)
            {
                ultraQualityWater.gameObject.SetActive(renderWater);
                Vector3 ultraQualityWaterPosition;
                if (cameraMode != CameraMode.Isometric)
                {
                    ultraQualityWaterPosition = new Vector3(cameraPosition.x, ultraQualityWater.transform.position.y, cameraPosition.z);
                }
                else
                {
                    ultraQualityWaterPosition = new Vector3(isoPosition.x, ultraQualityWater.transform.position.y, isoPosition.y);
                }

                ultraQualityWater.transform.position = ultraQualityWaterPosition;
                ultraQualityWater.Update();
            }
            else if (Properties.Instance.WaterQuality == Gui.WaterQuality.High)
            {
                highQualityWater.gameObject.SetActive(renderWater);
                if (cameraMode != CameraMode.Isometric)
                {
                    highQualityWater.transform.position = new Vector3(cameraPosition.x, ultraQualityWater.transform.position.y, cameraPosition.z);
                }
                else
                {
                    highQualityWater.transform.position = new Vector3(isoPosition.x, ultraQualityWater.transform.position.y, isoPosition.y);
                }
            }
            else if (Properties.Instance.WaterQuality == Gui.WaterQuality.Simple)
            {
                simpleQualityWater.gameObject.SetActive(renderWater);
            }
        }

        private void PrepareMapState()
        {
            Tab tab = LayoutManager.Instance.CurrentTab;
            bool forceSurfaceEditing = tab == Tab.Ground || tab == Tab.Height;
            int editingFloor = forceSurfaceEditing ? 0 : Floor;
            
            Map map = GameManager.Instance.Map;
            if (map.RenderedFloor != editingFloor)
            {
                map.RenderedFloor = editingFloor;
            }
            if (map.RenderEntireMap != RenderEntireMap)
            {
                map.RenderEntireMap = RenderEntireMap;
            }
            bool renderHeights = tab == Tab.Height;
            if (Floor < 0)
            {
                map.CaveGridMesh.HandlesVisible = renderHeights;
                map.CaveGridMesh.SetRenderHeightColors(renderHeights);
                map.CaveGridMesh.ApplyAllChanges();
            }
            else
            {
                map.SurfaceGridMesh.HandlesVisible = renderHeights;
                map.SurfaceGridMesh.SetRenderHeightColors(renderHeights);
                map.SurfaceGridMesh.ApplyAllChanges();
            }
        }
        
        private void PrepareProjector()
        {
            if (!CurrentRaycast.collider)
            {
                return;
            }
            
            GameObject hitObject = CurrentRaycast.collider.gameObject;
            bool gridOrGroundHit = hitObject.GetComponent<GroundMesh>() || hitObject.GetComponent<OverlayMesh>();
            if (!gridOrGroundHit)
            {
                return;
            }

            TileSelectionMode tileSelectionMode = LayoutManager.Instance.TileSelectionMode;
            Vector3 raycastPosition = CurrentRaycast.point;
            TileSelectionHit tileSelectionHit = TileSelection.PositionToTileSelectionHit(raycastPosition, tileSelectionMode);
            TileSelectionTarget target = tileSelectionHit.Target;

            if (target == TileSelectionTarget.Nothing)
            {
                return;
            }

            attachedProjector.gameObject.SetActive(true);

            int tileX = tileSelectionHit.X;
            int tileY = tileSelectionHit.Y;

            const float borderThickness = TileSelection.BorderThickness;

            switch (target)
            {
                case TileSelectionTarget.Tile:
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4 + 2);
                    attachedProjector.orthographicSize = 2;
                    attachedProjector.aspectRatio = 1;
                    break;
                case TileSelectionTarget.InnerTile:
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4 + 2);
                    attachedProjector.orthographicSize = 2 - borderThickness * 4;
                    attachedProjector.aspectRatio = 1;
                    break;
                case TileSelectionTarget.BottomBorder:
                    attachedProjector.transform.position = new Vector3(tileX * 4 + 2, 10000, tileY * 4);
                    attachedProjector.orthographicSize = borderThickness * 4;
                    attachedProjector.aspectRatio = 2f / (borderThickness * 4) - (borderThickness * 6);
                    break;
                case TileSelectionTarget.LeftBorder:
                    attachedProjector.transform.position = new Vector3(tileX * 4, 10000, tileY * 4 + 2);
                    attachedProjector.orthographicSize = 2 - (borderThickness * 4);
                    attachedProjector.aspectRatio = (borderThickness * 4) / 1.5f;
                    break;
                case TileSelectionTarget.Corner:
                    attachedProjector.transform.position = new Vector3(tileX * 4, 10000, tileY * 4);
                    attachedProjector.orthographicSize = borderThickness * 4;
                    attachedProjector.aspectRatio = 1;
                    break;
            }
        }

        private void UpdatePerspectiveCamera(Map map)
        {
            int activeWindow = LayoutManager.Instance.ActiveWindow;
            if (activeWindow == screenId)
            {
                float movementMultiplier = 1;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    movementMultiplier *= Properties.Instance.FppShiftModifier;
                }
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    movementMultiplier *= Properties.Instance.FppControlModifier;
                }
                
                Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                movement *= Properties.Instance.FppMovementSpeed * Time.deltaTime * movementMultiplier;

                if (Input.GetKey(KeyCode.Q))
                {
                    fppRotation += new Vector3(0, -Time.deltaTime * Properties.Instance.FppKeyboardRotationSensitivity, 0);
                    fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);
                }
                if (Input.GetKey(KeyCode.E))
                {
                    fppRotation += new Vector3(0, Time.deltaTime * Properties.Instance.FppKeyboardRotationSensitivity, 0);
                    fppRotation = new Vector3(Mathf.Clamp(fppRotation.x, -90, 90), fppRotation.y % 360, fppRotation.z);
                }

                if (Input.GetKey(KeyCode.R))
                {
                    fppPosition += new Vector3(0, Time.deltaTime * Properties.Instance.FppMovementSpeed * movementMultiplier, 0);
                }
                if (Input.GetKey(KeyCode.F))
                {
                    fppPosition += new Vector3(0, -Time.deltaTime * Properties.Instance.FppMovementSpeed * movementMultiplier, 0);
                }

                Transform attachedCameraTransform = AttachedCamera.transform;
                attachedCameraTransform.localPosition = fppPosition;
                attachedCameraTransform.Translate(movement, Space.Self);
                fppPosition = attachedCameraTransform.position;
            }

            if (CameraMode == CameraMode.Wurmian)
            {
                if (fppPosition.x < 0)
                {
                    fppPosition.x = 0;
                }
                if (fppPosition.z < 0)
                {
                    fppPosition.z = 0;
                }
                if (fppPosition.x > map.Width * 4)
                {
                    fppPosition.x = map.Width * 4;
                }
                if (fppPosition.z > map.Height * 4)
                {
                    fppPosition.z = map.Height * 4;
                }

                int currentTileX = (int) (fppPosition.x / 4f);
                int currentTileY = (int) (fppPosition.z / 4f);

                float xPart = (fppPosition.x % 4f) / 4f;
                float yPart = (fppPosition.z % 4f) / 4f;
                float xPartRev = 1f - xPart;
                float yPartRev = 1f - yPart;

                float h00 = map[currentTileX, currentTileY].GetHeightForFloor(floor) * 0.1f;
                float h10 = map[currentTileX + 1, currentTileY].GetHeightForFloor(floor) * 0.1f;
                float h01 = map[currentTileX, currentTileY + 1].GetHeightForFloor(floor) * 0.1f;
                float h11 = map[currentTileX + 1, currentTileY + 1].GetHeightForFloor(floor) * 0.1f;

                float x0 = (h00 * xPartRev + h10 * xPart);
                float x1 = (h01 * xPartRev + h11 * xPart);

                float height = (x0 * yPartRev + x1 * yPart);
                height += WurmianHeight;
                if (height < 0.3f)
                {
                    height = 0.3f;
                }
                fppPosition.y = height;
            }
        }

        private void UpdateTopCamera(Map map)
        {
            int activeWindow = LayoutManager.Instance.ActiveWindow;
            if (activeWindow == screenId)
            {
                if (MouseOver)
                {
                    Vector3 raycastPoint = CurrentRaycast.point;
                    Vector2 topPoint = new Vector2(raycastPoint.x, raycastPoint.z);
                    
                    float scroll = Input.mouseScrollDelta.y;
                    if (scroll > 0 && topScale > 10)
                    {
                        topPosition += (topPoint - topPosition) / topScale * 4;
                        topScale -= 4;
                    }
                    else if (scroll < 0)
                    {
                        topPosition -= (topPoint - topPosition) / topScale * 4;
                        topScale += 4;
                    }
                }

                Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                movement *= Properties.Instance.TopMovementSpeed * Time.deltaTime;
                topPosition += movement;
            }

            if (topPosition.x < topScale * AttachedCamera.aspect)
            {
                topPosition.x = topScale * AttachedCamera.aspect;
            }
            if (topPosition.y < topScale)
            {
                topPosition.y = topScale;
            }

            if (topPosition.x > map.Width * 4 - topScale * AttachedCamera.aspect)
            {
                topPosition.x = map.Width * 4 - topScale * AttachedCamera.aspect;
            }
            if (topPosition.y > map.Height * 4 - topScale)
            {
                topPosition.y = map.Height * 4 - topScale;
            }

            bool fitsHorizontally = map.Width * 2 < topScale * AttachedCamera.aspect;
            bool fitsVertically = map.Height * 2 < topScale;

            if (fitsHorizontally)
            {
                topPosition.x = map.Width * 2;
            }
            if (fitsVertically)
            {
                topPosition.y = map.Height * 2;
            }
        }

        private void UpdateIsometricCamera(Map map)
        {
            int activeWindow = LayoutManager.Instance.ActiveWindow;
            if (activeWindow == screenId)
            {
                if (MouseOver)
                {
                    float scroll = Input.mouseScrollDelta.y;
                    if (scroll > 0 && isoScale > 10)
                    {
                        isoScale -= 4;
                    }
                    else if (scroll < 0)
                    {
                        isoScale += 4;
                    }
                }

                Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                movement *= Properties.Instance.IsoMovementSpeed * Time.deltaTime;
                isoPosition += movement;
            }

            if (isoPosition.x < -(map.Width * 4 / Mathf.Sqrt(2) - isoScale * AttachedCamera.aspect))
            {
                isoPosition.x = -(map.Width * 4 / Mathf.Sqrt(2) - isoScale * AttachedCamera.aspect);
            }
            if (isoPosition.y < isoScale)
            {
                isoPosition.y = isoScale;
            }

            if (isoPosition.x > map.Width * 4 / Mathf.Sqrt(2) - isoScale * AttachedCamera.aspect)
            {
                isoPosition.x = map.Width * 4 / Mathf.Sqrt(2) - isoScale * AttachedCamera.aspect;
            }
            if (isoPosition.y > map.Height * 4 / Mathf.Sqrt(2) - isoScale)
            {
                isoPosition.y = map.Height * 4 / Mathf.Sqrt(2) - isoScale;
            }

            bool fitsHorizontally = map.Width * 2 * Mathf.Sqrt(2) < isoScale * AttachedCamera.aspect;
            bool fitsVertically = map.Height * 2 / Mathf.Sqrt(2) < isoScale;

            if (fitsHorizontally)
            {
                isoPosition.x = 0;
            }
            if (fitsVertically)
            {
                isoPosition.y = map.Height * 2 / Mathf.Sqrt(2);
            }
        }

        private void UpdateState()
        {
            Transform cameraTransform = AttachedCamera.transform;
            if (CameraMode == CameraMode.Perspective || CameraMode == CameraMode.Wurmian)
            {
                AttachedCamera.clearFlags = CameraClearFlags.Skybox;
                AttachedCamera.orthographic = false;
                cameraTransform.localPosition = fppPosition;
                cameraTransform.localRotation = Quaternion.Euler(fppRotation);
                parentTransform.localRotation = Quaternion.identity;
            }
            else if (cameraMode == CameraMode.Top)
            {
                AttachedCamera.clearFlags = CameraClearFlags.SolidColor;
                AttachedCamera.orthographic = true;
                AttachedCamera.orthographicSize = topScale;
                cameraTransform.localPosition = new Vector3(topPosition.x, 10000, topPosition.y);
                cameraTransform.localRotation = Quaternion.Euler(90, 0, 0);
                parentTransform.localRotation = Quaternion.identity;
            }
            else if (cameraMode == CameraMode.Isometric)
            {
                AttachedCamera.clearFlags = CameraClearFlags.SolidColor;
                AttachedCamera.orthographic = true;
                AttachedCamera.orthographicSize = isoScale;
                cameraTransform.localPosition = new Vector3(isoPosition.x, isoPosition.y, -10000);
                cameraTransform.localRotation = Quaternion.identity;
                parentTransform.localRotation = Quaternion.Euler(30, 45, 0);
            }
        }

        private void OnRenderObject()
        {
            Camera[] waterCameras = ultraQualityWater.GetComponentsInChildren<Camera>();
            bool currentWaterCamera = waterCameras.Contains(Camera.current);
            bool currentAttachedCamera = Camera.current == AttachedCamera;
            if (!currentWaterCamera && !currentAttachedCamera || !CurrentRaycast.collider)
            {
                return;
            }
            
            GameObject hitObject = CurrentRaycast.collider.gameObject;
            GroundMesh groundMesh = hitObject.GetComponent<GroundMesh>();
            OverlayMesh overlayMesh = hitObject.GetComponent<OverlayMesh>();
            HeightmapHandle heightmapHandle = GameManager.Instance.Map.SurfaceGridMesh.RaycastHandles();

            bool gridOrGroundHit = groundMesh || overlayMesh || heightmapHandle != null;

            if (!gridOrGroundHit)
            {
                GL.PushMatrix();
                GraphicsManager.Instance.SimpleDrawingMaterial.SetPass(0);
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(hitObject.transform.position, hitObject.transform.rotation, hitObject.transform.lossyScale);
                GL.MultMatrix(rotationMatrix);
                RenderRaytrace();
                GL.PopMatrix();
            }
        }

        private void RenderRaytrace()
        {
            Collider hitCollider = CurrentRaycast.collider;
            if (hitCollider == null)
            {
                return;
            }

            if (hitCollider.GetType() == typeof(MeshCollider))
            {
                MeshCollider meshCollider = (MeshCollider)hitCollider;
                Mesh mesh = meshCollider.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                if (normals == null || normals.Length == 0)
                {
                    normals = new Vector3[vertices.Length];
                }
                int[] triangles = mesh.triangles;
                GL.Begin(GL.TRIANGLES);
                GL.Color(pickerColor);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    GL.Vertex(vertices[triangles[i]] + normals[triangles[i]] * 0.05f);
                    GL.Vertex(vertices[triangles[i + 1]] + normals[triangles[i + 1]] * 0.05f);
                    GL.Vertex(vertices[triangles[i + 2]] + normals[triangles[i + 2]] * 0.05f);
                }
                GL.End();
            }
            else if (hitCollider.GetType() == typeof(BoxCollider))
            {
                BoxCollider boxCollider = (BoxCollider)hitCollider;
                Vector3 size = boxCollider.size * 1.01f;
                Vector3 center = boxCollider.center;

                Vector3 v000 = center + new Vector3(-size.x, -size.y, -size.z) / 2f;
                Vector3 v001 = center + new Vector3(-size.x, -size.y, size.z) / 2f;
                Vector3 v010 = center + new Vector3(-size.x, size.y, -size.z) / 2f;
                Vector3 v011 = center + new Vector3(-size.x, size.y, size.z) / 2f;
                Vector3 v100 = center + new Vector3(size.x, -size.y, -size.z) / 2f;
                Vector3 v101 = center + new Vector3(size.x, -size.y, size.z) / 2f;
                Vector3 v110 = center + new Vector3(size.x, size.y, -size.z) / 2f;
                Vector3 v111 = center + new Vector3(size.x, size.y, size.z) / 2f;

                GL.Begin(GL.QUADS);
                GL.Color(pickerColor);
                //bottom
                GL.Vertex(v000);
                GL.Vertex(v100);
                GL.Vertex(v101);
                GL.Vertex(v001);
                //top
                GL.Vertex(v111);
                GL.Vertex(v110);
                GL.Vertex(v010);
                GL.Vertex(v011);
                //down
                GL.Vertex(v110);
                GL.Vertex(v100);
                GL.Vertex(v000);
                GL.Vertex(v010);
                //up
                GL.Vertex(v001);
                GL.Vertex(v101);
                GL.Vertex(v111);
                GL.Vertex(v011);
                //left
                GL.Vertex(v000);
                GL.Vertex(v001);
                GL.Vertex(v011);
                GL.Vertex(v010);
                //right
                GL.Vertex(v111);
                GL.Vertex(v101);
                GL.Vertex(v100);
                GL.Vertex(v110);
                GL.End();
            }
        }

        private void OnPostRender()
        {
            attachedProjector.gameObject.SetActive(false);
            if (Properties.Instance.WaterQuality == Gui.WaterQuality.Ultra)
            {
                ultraQualityWater.gameObject.SetActive(false);
            }
        }
    }
}