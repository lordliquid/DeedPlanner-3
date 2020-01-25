﻿using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Logic;
using System.Linq;
using System;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Gui
{
    public class LayoutManager : MonoBehaviour
    {
        public static LayoutManager Instance { get; private set; }

        [SerializeField] private CanvasScaler mainCanvasScaler = null;
        
        [SerializeField] private Toggle[] indicatorButtons = new Toggle[4];
        [SerializeField] private RectTransform horizontalBottomIndicatorHolder = null;
        [SerializeField] private RawImage[] screens = new RawImage[4];
        [SerializeField] private RectTransform horizontalBottomScreenHolder = null;
        [SerializeField] private RectTransform[] splits = new RectTransform[5];
        [SerializeField] private MultiCamera[] cameras = new MultiCamera[4];
        [SerializeField] private ToggleGroup cameraModeGroup = null;
        [SerializeField] private Toggle[] cameraModeToggles = new Toggle[4];
        [SerializeField] private ToggleGroup floorGroup = null;
        [SerializeField] private FloorToggle[] positiveFloorToggles = new FloorToggle[16];
        [SerializeField] private FloorToggle[] negativeFloorToggles = new FloorToggle[6];

        [SerializeField] private TabObject[] tabs = new TabObject[12];
        [SerializeField] private Toggle groundToggle = null;
        [SerializeField] private Toggle cavesToggle = null;

        [SerializeField] private GameObject highQualityWaterObject = null;
        [SerializeField] private GameObject simpleQualityWaterObject = null;

        [SerializeField] private Tooltip tooltip = null;

        public event GenericEventArgs<Tab> TabChanged;

        private int activeWindow;
        private Layout currentLayout = Layout.Single;
        private Tab currentTab;
        public TileSelectionMode TileSelectionMode { get; set; }
        
        public MultiCamera CurrentCamera => cameras[ActiveWindow];

        public MultiCamera HoveredCamera
        {
            get
            {
                foreach (MultiCamera cam in cameras)
                {
                    if (cam.MouseOver)
                    {
                        return cam;
                    }
                }

                return null;
            }
        }

        public string TooltipText
        {
            get => tooltip.Value;
            set => tooltip.Value = value;
        }

        public RectTransform ActiveWindowTransform => screens[activeWindow].gameObject.GetComponent<RectTransform>();

        public int ActiveWindow {
            get => activeWindow;
            private set {
                activeWindow = value;

                int floor = cameras[ActiveWindow].Floor;
                foreach (FloorToggle toggle in positiveFloorToggles)
                {
                    toggle.Toggle.isOn = false;
                }
                foreach (FloorToggle toggle in negativeFloorToggles)
                {
                    toggle.Toggle.isOn = false;
                }

                if (floor < 0)
                {
                    floor++;
                    negativeFloorToggles[floor].Toggle.isOn = true;
                }
                else
                {
                    positiveFloorToggles[floor].Toggle.isOn = true;
                }

                CameraMode cameraMode = cameras[ActiveWindow].CameraMode;
                foreach (Toggle toggle in cameraModeToggles)
                {
                    toggle.isOn = false;
                    if (toggle.GetComponent<CameraModeReference>().CameraMode == cameraMode)
                    {
                        toggle.isOn = true;
                    }
                }
            }
        }

        public Tab CurrentTab {
            get => currentTab;
            set {
                currentTab = value;
                foreach (TabObject tabObject in tabs)
                {
                    tabObject.Object.SetActive(tabObject.Tab == currentTab);
                }
                TabChanged?.Invoke(currentTab);
            }
        }

        public LayoutManager()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // state validation at launch - it makes development and debugging easier as you don't need to make sure tab is set to the proper one when commiting
            CurrentTab = currentTab;

            Properties.Instance.Saved += ValidateState;
            ValidateState();
        }

        private void ValidateState()
        {
            WaterQuality waterQuality = Properties.Instance.WaterQuality;
            highQualityWaterObject.SetActive(waterQuality == WaterQuality.High);
            simpleQualityWaterObject.SetActive(waterQuality == WaterQuality.Simple);
        }

        public void UpdateCanvasScale()
        {
            float referenceWidth = Constants.DefaultGuiWidth;
            float referenceHeight = Constants.DefaultGuiHeight * (Properties.Instance.GuiScale * Constants.GuiScaleUnitsToRealScale);
            mainCanvasScaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
        }
        
        public void OnLayoutChange(LayoutReference layoutReference)
        {
            Layout layout = layoutReference.Layout;
            currentLayout = layout;

            switch (layout)
            {
                case Layout.Single:
                    cameras[0].gameObject.SetActive(true);
                    cameras[1].gameObject.SetActive(false);
                    cameras[2].gameObject.SetActive(false);
                    cameras[3].gameObject.SetActive(false);
                    indicatorButtons[0].gameObject.SetActive(true);
                    indicatorButtons[1].gameObject.SetActive(false);
                    indicatorButtons[2].gameObject.SetActive(false);
                    indicatorButtons[3].gameObject.SetActive(false);
                    horizontalBottomIndicatorHolder.gameObject.SetActive(false);
                    screens[0].gameObject.SetActive(true);
                    screens[1].gameObject.SetActive(false);
                    screens[2].gameObject.SetActive(false);
                    screens[3].gameObject.SetActive(false);
                    horizontalBottomScreenHolder.gameObject.SetActive(false);
                    splits[0].gameObject.SetActive(false);
                    splits[1].gameObject.SetActive(false);
                    splits[2].gameObject.SetActive(false);
                    splits[3].gameObject.SetActive(false);
                    splits[4].gameObject.SetActive(false);
                    if (ActiveWindow != 0)
                    {
                        indicatorButtons[ActiveWindow].isOn = false;
                        ActiveWindow = 0;
                        indicatorButtons[0].isOn = true;
                    }
                    break;
                case Layout.HorizontalSplit:
                    cameras[0].gameObject.SetActive(true);
                    cameras[1].gameObject.SetActive(false);
                    cameras[2].gameObject.SetActive(true);
                    cameras[3].gameObject.SetActive(false);
                    indicatorButtons[0].gameObject.SetActive(true);
                    indicatorButtons[1].gameObject.SetActive(false);
                    indicatorButtons[2].gameObject.SetActive(true);
                    indicatorButtons[3].gameObject.SetActive(false);
                    horizontalBottomIndicatorHolder.gameObject.SetActive(true);
                    screens[0].gameObject.SetActive(true);
                    screens[1].gameObject.SetActive(false);
                    screens[2].gameObject.SetActive(true);
                    screens[3].gameObject.SetActive(false);
                    horizontalBottomScreenHolder.gameObject.SetActive(true);
                    splits[0].gameObject.SetActive(true);
                    splits[1].gameObject.SetActive(false);
                    splits[2].gameObject.SetActive(false);
                    splits[3].gameObject.SetActive(false);
                    splits[4].gameObject.SetActive(false);
                    if (ActiveWindow != 0 && ActiveWindow != 2)
                    {
                        indicatorButtons[ActiveWindow].isOn = false;
                        ActiveWindow = 0;
                        indicatorButtons[0].isOn = true;
                    }
                    break;
                case Layout.VerticalSplit:
                    cameras[0].gameObject.SetActive(true);
                    cameras[1].gameObject.SetActive(true);
                    cameras[2].gameObject.SetActive(false);
                    cameras[3].gameObject.SetActive(false);
                    indicatorButtons[0].gameObject.SetActive(true);
                    indicatorButtons[1].gameObject.SetActive(true);
                    indicatorButtons[2].gameObject.SetActive(false);
                    indicatorButtons[3].gameObject.SetActive(false);
                    horizontalBottomIndicatorHolder.gameObject.SetActive(false);
                    screens[0].gameObject.SetActive(true);
                    screens[1].gameObject.SetActive(true);
                    screens[2].gameObject.SetActive(false);
                    screens[3].gameObject.SetActive(false);
                    horizontalBottomScreenHolder.gameObject.SetActive(false);
                    splits[0].gameObject.SetActive(false);
                    splits[1].gameObject.SetActive(true);
                    splits[2].gameObject.SetActive(false);
                    splits[3].gameObject.SetActive(false);
                    splits[4].gameObject.SetActive(false);
                    if (ActiveWindow != 0 && ActiveWindow != 1)
                    {
                        indicatorButtons[ActiveWindow].isOn = false;
                        ActiveWindow = 0;
                        indicatorButtons[0].isOn = true;
                    }
                    break;
                case Layout.HorizontalTop:
                    cameras[0].gameObject.SetActive(true);
                    cameras[1].gameObject.SetActive(false);
                    cameras[2].gameObject.SetActive(true);
                    cameras[3].gameObject.SetActive(true);
                    indicatorButtons[0].gameObject.SetActive(true);
                    indicatorButtons[1].gameObject.SetActive(false);
                    indicatorButtons[2].gameObject.SetActive(true);
                    indicatorButtons[3].gameObject.SetActive(true);
                    horizontalBottomIndicatorHolder.gameObject.SetActive(true);
                    screens[0].gameObject.SetActive(true);
                    screens[1].gameObject.SetActive(false);
                    screens[2].gameObject.SetActive(true);
                    screens[3].gameObject.SetActive(true);
                    horizontalBottomScreenHolder.gameObject.SetActive(true);
                    splits[0].gameObject.SetActive(false);
                    splits[1].gameObject.SetActive(false);
                    splits[2].gameObject.SetActive(true);
                    splits[3].gameObject.SetActive(false);
                    splits[4].gameObject.SetActive(false);
                    if (ActiveWindow != 0 && ActiveWindow != 2 && ActiveWindow != 3)
                    {
                        indicatorButtons[ActiveWindow].isOn = false;
                        ActiveWindow = 0;
                        indicatorButtons[0].isOn = true;
                    }
                    break;
                case Layout.HorizontalBottom:
                    cameras[0].gameObject.SetActive(true);
                    cameras[1].gameObject.SetActive(true);
                    cameras[2].gameObject.SetActive(true);
                    cameras[3].gameObject.SetActive(false);
                    indicatorButtons[0].gameObject.SetActive(true);
                    indicatorButtons[1].gameObject.SetActive(true);
                    indicatorButtons[2].gameObject.SetActive(true);
                    indicatorButtons[3].gameObject.SetActive(false);
                    horizontalBottomIndicatorHolder.gameObject.SetActive(true);
                    screens[0].gameObject.SetActive(true);
                    screens[1].gameObject.SetActive(true);
                    screens[2].gameObject.SetActive(true);
                    screens[3].gameObject.SetActive(false);
                    horizontalBottomScreenHolder.gameObject.SetActive(true);
                    splits[0].gameObject.SetActive(false);
                    splits[1].gameObject.SetActive(false);
                    splits[2].gameObject.SetActive(false);
                    splits[3].gameObject.SetActive(true);
                    splits[4].gameObject.SetActive(false);
                    if (ActiveWindow != 0 && ActiveWindow != 1 && ActiveWindow != 2)
                    {
                        indicatorButtons[ActiveWindow].isOn = false;
                        ActiveWindow = 0;
                        indicatorButtons[0].isOn = true;
                    }
                    break;
                case Layout.Quad:
                    cameras[0].gameObject.SetActive(true);
                    cameras[1].gameObject.SetActive(true);
                    cameras[2].gameObject.SetActive(true);
                    cameras[3].gameObject.SetActive(true);
                    indicatorButtons[0].gameObject.SetActive(true);
                    indicatorButtons[1].gameObject.SetActive(true);
                    indicatorButtons[2].gameObject.SetActive(true);
                    indicatorButtons[3].gameObject.SetActive(true);
                    horizontalBottomIndicatorHolder.gameObject.SetActive(true);
                    screens[0].gameObject.SetActive(true);
                    screens[1].gameObject.SetActive(true);
                    screens[2].gameObject.SetActive(true);
                    screens[3].gameObject.SetActive(true);
                    horizontalBottomScreenHolder.gameObject.SetActive(true);
                    splits[0].gameObject.SetActive(false);
                    splits[1].gameObject.SetActive(false);
                    splits[2].gameObject.SetActive(false);
                    splits[3].gameObject.SetActive(false);
                    splits[4].gameObject.SetActive(true);
                    break;
            }
        }

        public void OnActiveIndicatorChange(int window)
        {
            if (ActiveWindow == window)
            {
                return;
            }

            if (indicatorButtons[window].isOn)
            {
                ActiveWindow = window;
                Debug.Log("Active window changed to " + ActiveWindow);
            }
        }

        public void OnActiveWindowChange(int window)
        {
            if (ActiveWindow == window)
            {
                return;
            }

            indicatorButtons[ActiveWindow].isOn = false;
            indicatorButtons[window].isOn = true;
            ActiveWindow = window;
            Debug.Log("Active window changed to " + ActiveWindow);
        }

        public void OnCameraModeChange()
        {
            CameraModeReference cameraModeReference = cameraModeGroup.ActiveToggles().First().GetComponent<CameraModeReference>();
            CameraMode cameraMode = cameraModeReference.CameraMode;

            if (cameras[ActiveWindow].CameraMode == cameraMode)
            {
                return;
            }

            cameras[ActiveWindow].CameraMode = cameraMode;
            Debug.Log("Camera " + ActiveWindow + " camera mode changed to " + cameraMode);
        }

        public void OnFloorChange()
        {
            FloorToggle floorToggle = floorGroup.ActiveToggles().First().GetComponent<FloorToggle>();
            int floor = floorToggle.Floor;

            if (cameras[ActiveWindow].Floor == floor)
            {
                return;
            }

            cameras[ActiveWindow].Floor = floor;
            Debug.Log("Camera " + ActiveWindow + " floor changed to " + floor);
            UpdateTabs();
        }

        public void OnTabChange(TabReference tabReference)
        {
            Tab tab = tabReference.Tab;
            CurrentTab = tab;
            UpdateTabs();
        }

        private void UpdateTabs()
        {
            int floor = floorGroup.ActiveToggles().First().GetComponent<FloorToggle>().Floor;
            if (floor < 0)
            {
                groundToggle.gameObject.SetActive(false);
                cavesToggle.gameObject.SetActive(true);
                if (groundToggle.isOn)
                {
                    FindObjectForTab(Tab.Ground).SetActive(false);
                    FindObjectForTab(Tab.Caves).SetActive(true);
                    groundToggle.isOn = false;
                    cavesToggle.isOn = true;
                    CurrentTab = Tab.Caves;
                }
            }
            else if (floor >= 0)
            {
                groundToggle.gameObject.SetActive(true);
                cavesToggle.gameObject.SetActive(false);
                if (cavesToggle.isOn)
                {
                    FindObjectForTab(Tab.Ground).SetActive(true);
                    FindObjectForTab(Tab.Caves).SetActive(false);
                    groundToggle.isOn = true;
                    cavesToggle.isOn = false;
                    CurrentTab = Tab.Ground;
                }
            }
        }

        private GameObject FindObjectForTab(Tab tab)
        {
            foreach (TabObject tabObject in tabs)
            {
                if (tabObject.Tab == tab)
                {
                    return tabObject.Object;
                }
            }

            return null;
        }

    }

    [Serializable]
    public struct TabObject
    {
        public Tab Tab;
        public GameObject Object;
    }

}