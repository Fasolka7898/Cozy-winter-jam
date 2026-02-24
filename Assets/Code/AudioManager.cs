using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class AdvancedMusicManager : MonoBehaviour
{
    public static AdvancedMusicManager Instance;

    [Header("Audio Sources")]
    public List<AudioSource> audioSources;

    [Header("Scene Music Settings")]
    public List<SceneMusicConfig> sceneConfigs;

    [System.Serializable]
    public class SceneMusicConfig
    {
        public string sceneName;
        public List<MusicLayer> layers;
        public bool crossfade = true;
        public float fadeTime = 2f;
    }

    [System.Serializable]
    public class MusicLayer
    {
        public string layerName;
        public List<AudioClip> clips; // Теперь список клипов
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = true;
        public bool randomize = false; // ГАЛОЧКА ДЛЯ РАНДОМА
        public bool playInSequence = false; // Или по порядку
        public float minDelayBetweenClips = 0f; // Для неповторяющихся звуков
        public float maxDelayBetweenClips = 0f;

        // Для отслеживания текущего индекса при последовательном проигрывании
        [System.NonSerialized] public int currentClipIndex = 0;
        [System.NonSerialized] public float nextPlayTime = 0f;
        [System.NonSerialized] public bool isPlayingRandom = false;
    }

    private Dictionary<string, SceneMusicConfig> sceneMusicMap;
    private List<AudioSource> activeSources = new List<AudioSource>();
    private List<AudioSource> fadingOutSources = new List<AudioSource>();
    private Coroutine transitionRoutine;
    private string currentScene = "";
    private Dictionary<AudioSource, Coroutine> randomPlayRoutines = new Dictionary<AudioSource, Coroutine>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            BuildSceneMap();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void InitializeAudioSources()
    {
        // Создаем AudioSource если их нет
        if (audioSources == null)
            audioSources = new List<AudioSource>();

        if (audioSources.Count == 0)
        {
            // Создаем минимум 8 источников для переходов
            int sourceCount = 8;
            for (int i = 0; i < sourceCount; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                audioSources.Add(source);
            }
        }
        else
        {
            // Настраиваем существующие
            foreach (var source in audioSources)
            {
                if (source != null)
                {
                    source.playOnAwake = false;
                    source.loop = true;
                }
            }
        }
    }

    private void BuildSceneMap()
    {
        sceneMusicMap = new Dictionary<string, SceneMusicConfig>();
        foreach (var config in sceneConfigs)
        {
            if (!string.IsNullOrEmpty(config.sceneName) && !sceneMusicMap.ContainsKey(config.sceneName))
            {
                sceneMusicMap.Add(config.sceneName, config);
            }
        }
    }

    private void Start()
    {
        // Запускаем музыку для начальной сцены
        string startScene = SceneManager.GetActiveScene().name;
        PlaySceneMusic(startScene);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneMusic(scene.name);
    }

    private void PlaySceneMusic(string sceneName)
    {
        // Не перезапускаем ту же сцену
        if (currentScene == sceneName && transitionRoutine != null)
            return;

        currentScene = sceneName;

        // Останавливаем все корутины случайного воспроизведения
        StopAllRandomPlayRoutines();

        // Ищем конфиг для сцены
        if (sceneMusicMap.TryGetValue(sceneName, out SceneMusicConfig config))
        {
            // Останавливаем текущий переход если есть
            if (transitionRoutine != null)
                StopCoroutine(transitionRoutine);

            // Запускаем новый переход
            transitionRoutine = StartCoroutine(TransitionMusic(config));
        }
        else
        {
            Debug.LogWarning($"Нет музыкальной конфигурации для сцены: {sceneName}");
            // Если конфига нет, просто затухаем всю музыку
            StartCoroutine(FadeOutAllMusic());
        }
    }

    private void StopAllRandomPlayRoutines()
    {
        foreach (var kvp in randomPlayRoutines)
        {
            if (kvp.Value != null)
                StopCoroutine(kvp.Value);
        }
        randomPlayRoutines.Clear();
    }

    private IEnumerator TransitionMusic(SceneMusicConfig config)
    {
        // Сохраняем текущие играющие источники
        List<AudioSource> oldSources = new List<AudioSource>(activeSources);

        // Останавливаем случайное воспроизведение на старых источниках
        foreach (var source in oldSources)
        {
            if (randomPlayRoutines.ContainsKey(source))
            {
                StopCoroutine(randomPlayRoutines[source]);
                randomPlayRoutines.Remove(source);
            }
        }

        // Находим свободные источники для новой музыки
        List<AudioSource> newSources = new List<AudioSource>();

        // Сначала берем источники, которые не играют
        foreach (var source in audioSources)
        {
            if (!source.isPlaying && !newSources.Contains(source))
            {
                newSources.Add(source);
                if (newSources.Count >= config.layers.Count)
                    break;
            }
        }

        // Если не хватает, берем любые другие (кроме играющих сейчас)
        if (newSources.Count < config.layers.Count)
        {
            foreach (var source in audioSources)
            {
                if (!oldSources.Contains(source) && !newSources.Contains(source))
                {
                    newSources.Add(source);
                    if (newSources.Count >= config.layers.Count)
                        break;
                }
            }
        }

        // Если все еще не хватает, создаем временные
        while (newSources.Count < config.layers.Count)
        {
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = true;
            audioSources.Add(newSource);
            newSources.Add(newSource);
        }

        // Настраиваем и запускаем новые источники
        for (int i = 0; i < config.layers.Count; i++)
        {
            var layer = config.layers[i];
            var source = newSources[i];

            if (layer.clips != null && layer.clips.Count > 0)
            {
                // Сбрасываем состояние слоя
                layer.currentClipIndex = 0;
                layer.nextPlayTime = Time.time;

                if (layer.randomize)
                {
                    // Для рандома - запускаем специальную корутину
                    source.loop = false; // Отключаем loop, управляем сами
                    source.volume = config.crossfade ? 0f : layer.volume;

                    Coroutine routine = StartCoroutine(RandomPlayRoutine(source, layer, config));
                    randomPlayRoutines[source] = routine;
                }
                else if (layer.clips.Count > 1 && !layer.loop)
                {
                    // Несколько клипов без повтора - проигрываем по порядку один раз
                    source.loop = false;
                    source.volume = config.crossfade ? 0f : layer.volume;
                    source.clip = layer.clips[0];
                    source.Play();

                    // Запускаем корутину для последовательного проигрывания
                    Coroutine routine = StartCoroutine(SequencePlayRoutine(source, layer, config));
                    randomPlayRoutines[source] = routine;
                }
                else
                {
                    // Обычное воспроизведение (один клип с loop)
                    source.loop = layer.loop;
                    source.clip = layer.clips[0];
                    source.volume = config.crossfade ? 0f : layer.volume;
                    source.Play();
                }
            }
        }

        // Обновляем активные источники
        activeSources = newSources.GetRange(0, config.layers.Count);

        // Если нужен перекрестный переход
        if (config.crossfade && oldSources.Count > 0)
        {
            float elapsed = 0f;
            float fadeTime = config.fadeTime > 0 ? config.fadeTime : 2f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;

                // Затухание старых источников
                for (int i = 0; i < oldSources.Count; i++)
                {
                    if (oldSources[i] != null && oldSources[i].isPlaying)
                    {
                        oldSources[i].volume = Mathf.Lerp(oldSources[i].volume, 0f, t);
                    }
                }

                // Нарастание новых
                for (int i = 0; i < activeSources.Count; i++)
                {
                    if (i < config.layers.Count && config.layers[i].clips != null && config.layers[i].clips.Count > 0)
                    {
                        activeSources[i].volume = Mathf.Lerp(0f, config.layers[i].volume, t);
                    }
                }

                yield return null;
            }

            // Останавливаем старые источники
            foreach (var source in oldSources)
            {
                if (source != null)
                {
                    source.Stop();
                    source.clip = null;
                }
            }

            // Устанавливаем финальную громкость новых
            for (int i = 0; i < activeSources.Count; i++)
            {
                if (i < config.layers.Count && config.layers[i].clips != null && config.layers[i].clips.Count > 0)
                {
                    activeSources[i].volume = config.layers[i].volume;
                }
            }
        }
        else if (config.crossfade)
        {
            // Нет старых источников - просто наращиваем громкость
            float elapsed = 0f;
            float fadeTime = config.fadeTime > 0 ? config.fadeTime : 2f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;

                for (int i = 0; i < activeSources.Count; i++)
                {
                    if (i < config.layers.Count && config.layers[i].clips != null && config.layers[i].clips.Count > 0)
                    {
                        activeSources[i].volume = Mathf.Lerp(0f, config.layers[i].volume, t);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < activeSources.Count; i++)
            {
                if (i < config.layers.Count && config.layers[i].clips != null && config.layers[i].clips.Count > 0)
                {
                    activeSources[i].volume = config.layers[i].volume;
                }
            }
        }
        else
        {
            // Без crossfade - сразу ставим громкость
            for (int i = 0; i < activeSources.Count; i++)
            {
                if (i < config.layers.Count && config.layers[i].clips != null && config.layers[i].clips.Count > 0)
                {
                    activeSources[i].volume = config.layers[i].volume;
                }
            }

            // Останавливаем старые источники
            foreach (var source in oldSources)
            {
                if (source != null)
                {
                    source.Stop();
                    source.clip = null;
                }
            }
        }

        transitionRoutine = null;
    }

    private IEnumerator RandomPlayRoutine(AudioSource source, MusicLayer layer, SceneMusicConfig config)
    {
        layer.isPlayingRandom = true;

        while (true)
        {
            if (layer.clips.Count == 0)
                yield break;

            // Выбираем случайный клип
            int randomIndex = Random.Range(0, layer.clips.Count);
            AudioClip clipToPlay = layer.clips[randomIndex];

            // Проигрываем
            source.clip = clipToPlay;
            source.Play();

            // Ждем окончания текущего клипа
            yield return new WaitForSeconds(clipToPlay.length);

            // Если есть задержка между клипами
            if (layer.maxDelayBetweenClips > 0)
            {
                float delay = Random.Range(layer.minDelayBetweenClips, layer.maxDelayBetweenClips);
                yield return new WaitForSeconds(delay);
            }
        }
    }

    private IEnumerator SequencePlayRoutine(AudioSource source, MusicLayer layer, SceneMusicConfig config)
    {
        int currentIndex = 0;

        while (currentIndex < layer.clips.Count)
        {
            AudioClip clipToPlay = layer.clips[currentIndex];

            source.clip = clipToPlay;
            source.Play();

            // Ждем окончания клипа
            yield return new WaitForSeconds(clipToPlay.length);

            // Если есть задержка
            if (layer.maxDelayBetweenClips > 0 && currentIndex < layer.clips.Count - 1)
            {
                float delay = Random.Range(layer.minDelayBetweenClips, layer.maxDelayBetweenClips);
                yield return new WaitForSeconds(delay);
            }

            currentIndex++;
        }

        // Если нужно зациклить всю последовательность
        if (layer.loop)
        {
            // Запускаем снова
            Coroutine newRoutine = StartCoroutine(SequencePlayRoutine(source, layer, config));
            randomPlayRoutines[source] = newRoutine;
        }
    }

    private IEnumerator FadeOutAllMusic()
    {
        List<AudioSource> sourcesToFade = new List<AudioSource>(activeSources);

        if (sourcesToFade.Count > 0)
        {
            float elapsed = 0f;
            float fadeTime = 2f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;

                foreach (var source in sourcesToFade)
                {
                    if (source != null && source.isPlaying)
                    {
                        source.volume = Mathf.Lerp(source.volume, 0f, t);
                    }
                }

                yield return null;
            }

            foreach (var source in sourcesToFade)
            {
                if (source != null)
                {
                    source.Stop();
                    source.clip = null;
                }
            }

            activeSources.Clear();
        }
    }

    // Публичные методы для управления музыкой
    public void SetMusicVolume(float volume)
    {
        foreach (var source in activeSources)
        {
            source.volume = volume;
        }
    }

    public void PauseMusic()
    {
        foreach (var source in audioSources)
        {
            if (source.isPlaying)
                source.Pause();
        }
    }

    public void ResumeMusic()
    {
        foreach (var source in audioSources)
        {
            if (source.clip != null && !source.isPlaying)
                source.Play();
        }
    }

    public void StopMusic()
    {
        StopAllRandomPlayRoutines();
        StartCoroutine(FadeOutAllMusic());
    }
}