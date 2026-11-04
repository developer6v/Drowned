using Drowned.Resources;
using Drowned.World;
using UnityEngine;

namespace Drowned.Core
{
    /// <summary>
    /// Toca os sons do jogo por evento (SCRUM-187). Um único objeto "AudioManager" na cena; os clips são
    /// arrastados no inspetor (placeholders em <c>Assets/Audio/Placeholder</c>, finais nas SCRUM-158/159/172/173/186).
    /// Clip vazio não dá erro: só não toca. Volumes em PlayerPrefs para a tela de opções (SCRUM-180).
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public const string SfxVolumeKey = "drowned.volume.sfx";
        public const string MusicVolumeKey = "drowned.volume.music";

        public static AudioManager Instance { get; private set; }

        [Header("Interface")]
        [SerializeField] private AudioClip _click;
        [SerializeField] private AudioClip _buildConfirm;
        [SerializeField] private AudioClip _error;

        [Header("Gameplay")]
        [SerializeField] private AudioClip _construction;
        [SerializeField] private AudioClip _unlock;
        [SerializeField] private AudioClip _crisisAlarm;
        [SerializeField] private AudioClip _monumentStage;
        [SerializeField] private AudioClip _victory;
        [SerializeField] private AudioClip _defeat;

        [Header("Ambiente")]
        [SerializeField] private AudioClip _ambientLoop;

        private AudioSource _sfx;
        private AudioSource _ambient;

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
            set
            {
                PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
                if (Instance != null) Instance.ApplyVolumes();
            }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, 0.6f);
            set
            {
                PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
                if (Instance != null) Instance.ApplyVolumes();
            }
        }

        /// <summary>Toca se houver AudioManager na cena. Seguro chamar de qualquer lugar.</summary>
        public static void PlayIfAvailable(GameSound sound)
        {
            if (Instance != null) Instance.Play(sound);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.playOnAwake = false;
            _ambient.loop = true;
            ApplyVolumes();
        }

        private void Start()
        {
            if (_ambientLoop != null)
            {
                _ambient.clip = _ambientLoop;
                _ambient.Play();
            }

            var gm = GameManagerBehaviour.Instance;
            if (gm == null || gm.Game == null) return;
            var game = gm.Game;

            game.Specialists.OnSpecialistUnlocked += _ => Play(GameSound.Unlock);
            game.Research.OnResearched += _ => Play(GameSound.Unlock);
            game.Monument.OnStageCompleted += _ => Play(GameSound.MonumentStage);
            game.Resources.OnDepleted += type =>
            {
                if (type == ResourceType.Energy || type == ResourceType.Oxygen) Play(GameSound.CrisisAlarm);
            };
            game.OnStateChanged += state => Play(state == GameState.Victory ? GameSound.Victory : GameSound.Defeat);

            var build = BuildModeController.Instance;
            if (build != null)
            {
                build.OnBuilt += _ => { Play(GameSound.BuildConfirm); Play(GameSound.Construction); };
                build.OnBuildFailed += (_, __) => Play(GameSound.Error);
            }
            var selector = StructureSelector.Instance;
            if (selector != null) selector.OnActionFailed += (_, __) => Play(GameSound.Error);
        }

        public void Play(GameSound sound)
        {
            var clip = ClipFor(sound);
            if (clip == null || _sfx == null) return;
            _sfx.PlayOneShot(clip, SfxVolume);
        }

        private AudioClip ClipFor(GameSound sound) => sound switch
        {
            GameSound.Click => _click,
            GameSound.BuildConfirm => _buildConfirm,
            GameSound.Error => _error,
            GameSound.Construction => _construction,
            GameSound.Unlock => _unlock,
            GameSound.CrisisAlarm => _crisisAlarm,
            GameSound.MonumentStage => _monumentStage,
            GameSound.Victory => _victory,
            GameSound.Defeat => _defeat,
            _ => null
        };

        private void ApplyVolumes()
        {
            if (_ambient != null) _ambient.volume = MusicVolume;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
