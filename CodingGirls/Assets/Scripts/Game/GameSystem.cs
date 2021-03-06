using UnityEngine;
using UnityEngine.SceneManagement;
using live2d;
using live2d.framework;
using System.Collections;

namespace Game
{
    public class GameSystem : MonoBehaviour
    {
        public static GameSystem _Instance { get; private set; }
        public ScenarioInfo _ScenarioInfo { get; private set; }
        private ScenarioManager _scenarioManager = new ScenarioManager();
        public ScenarioManager _ScenarioManager { get { return _scenarioManager; } }
        public UIManager _UI { get; private set; }
        public Background _Background { get; private set; }
        public Foreground _Foreground { get; private set; }
        private ModelManager _modelManager = new ModelManager();
        public ModelManager _ModelManager { get { return _modelManager; } }
        private SpriteManager _spriteManager = new SpriteManager();
        public SpriteManager _SpriteManager { get { return _spriteManager; } }
        public Presentation _Presentation { get; set; }
        public bool _DoingTask { get; set; }
        public bool _IsClicked { get; private set; }
        private delegate void UpdateFunc();
        private UpdateFunc _updateHandle = null;
        private static int _ReferenceResolutionWidth { get { return 1280; } }
        private static int _ReferenceResolutionHeight { get { return 720; } }

        /// <summary>
        /// 기준 해상도 비율. 너비/높이
        /// </summary>
        private static float _ReferenceResolutionRatio
        {
            get
            {
                return (float)_ReferenceResolutionWidth / _ReferenceResolutionHeight;
            }
        }

        public static float _ReferenceWorldHeight { get { return 2.0f; } }

        private static float _ReferenceWorldWidth
        {
            get
            {
                return _ReferenceWorldHeight * _ReferenceResolutionRatio;
            }
        }

        private void Initialize()
        {
            _Instance = this;
            App.AppSystem.TryInitializeApplication();
            SoundManager.Create();
            InitializeCamera();
            InitializeUIManager();
            InitializeBackground();
            InitializeForeground();
            InitializeL2D();
            _ScenarioManager.Initialize();
            _updateHandle = UpdateEmpty;
            StartCoroutine(Loading());
        }

        private void Clear()
        {
            ClearL2D();
            if (SoundManager._Instance != null)
            {
                SoundManager._Instance.StopBGM();
                SoundManager._Instance.ClearLoadedAudioClip();
            }
            _Instance = null;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void Update()
        {
            _updateHandle();
            _UI.OnKeyInput();
        }

        private void InitializeCamera()
        {
            if (Camera.main.aspect < _ReferenceResolutionRatio)
            {
                // 카메라 가로 비율이 기준보다 작다면
                // 예: 아이패드 프로 = 4:3, 기준 = 16:9
                // 카메라 가로가 기준 가로를 커버할 수 있도록
                Camera.main.orthographicSize = _ReferenceWorldWidth / 2.0f / Camera.main.aspect;
            }
        }

        private void InitializeUIManager()
        {
            _UI = FindObjectOfType<UIManager>();
            _UI.Initialize();
        }

        private void InitializeBackground()
        {
            Background prefab = Resources.Load<Background>(Define._backgroundPrefabPath);
            _Background = Instantiate<Background>(prefab);
            _Background.Initialize();
            _Background.SetColor(Color.black);
        }

        private void InitializeForeground()
        {
            Foreground prefab = Resources.Load<Foreground>(Define._foregroundPrefabPath);
            _Foreground = Instantiate<Foreground>(prefab);
            _Foreground.Initialize();
            _Foreground.SetActivate(false);
        }

        private void InitializeL2D()
        {
            Live2D.init();
            Live2DFramework.setPlatformManager(new PlatformManager());
        }

        private void ClearL2D()
        {
            Live2DFramework.setPlatformManager(null);
            Live2D.dispose();
        }

        private IEnumerator Loading()
        {
            LoadScenarioInfo();
            _ScenarioManager.Load(GetStartScenarioName());
            yield return null;

            // 로딩 종료
            _updateHandle = UpdateGame;
        }

        private string GetStartScenarioName()
        {
            if (!string.IsNullOrEmpty(App.AppSystem._GameSystemParam._ScenarioName))
            {
                return App.AppSystem._GameSystemParam._ScenarioName;
            }
            else
            {
                return _ScenarioInfo._defaultMainFileName;
            }
        }

        private void LoadScenarioInfo()
        {
            TextAsset loadedText = Resources.Load<TextAsset>(Define._scenarioInfoPath);
            ScenarioInfoJson loadedJson = JsonUtility.FromJson<ScenarioInfoJson>(loadedText.text);
            _ScenarioInfo = ScenarioInfo.ConvertFromJson(loadedJson);
        }

        private void UpdateEmpty()
        {
        }

        private void UpdateGame()
        {
            UpdateInput();

            while (!_DoingTask && _ScenarioManager.HasRemainedCommand())
            {
                _ScenarioManager.UpdateCommand();
            }
        }

        private void UpdateInput()
        {
            _IsClicked = _UI._Input.PopIsClicked();
        }

        public void RegisterTask(IEnumerator task)
        {
            StartCoroutine(DoTask(task));
        }

        private IEnumerator DoTask(IEnumerator task)
        {
            _DoingTask = true;
            yield return StartCoroutine(task);
            _DoingTask = false;
        }

        public void Wait(float duration)
        {
            RegisterTask(WaitTask(duration));
        }

        private IEnumerator WaitTask(float duration)
        {
            float startTime = Time.time;
            while (Time.time - (startTime + duration) < 0.0f)
            {
                yield return null;
            }
        }

        public void WaitClick()
        {
            RegisterTask(WaitClickTask());
        }

        private IEnumerator WaitClickTask()
        {
            while (!_IsClicked)
            {
                yield return null;
            }
        }

        public void LoadTitleScene()
        {
            SceneManager.LoadScene(Define.SceneName._title);
        }
    }
}