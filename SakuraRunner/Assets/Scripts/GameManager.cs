using System;
using System.Collections;
using UnityEngine;

namespace SakuraRunner
{
    public enum GameState { Menu, Playing, Paused, GameOver }

    /// <summary>Owns game state, score, speed and the run lifecycle.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public const float BaseSpeed = 13f;
        public const float MaxSpeed = 30f;
        const string BestKey = "sakura_best_score";

        public GameState State { get; private set; } = GameState.Menu;
        public float Speed { get; private set; }
        public float Distance { get; private set; }
        public int Flowers { get; private set; }
        public int Best { get; private set; }
        public bool NewBest { get; private set; }
        public int Score => Mathf.FloorToInt(Distance) + Flowers * 10;

        public PlayerController Player;
        public WorldGenerator World;

        public event Action<GameState> StateChanged;
        public event Action<int> FlowerCollected;

        float stateTime;

        void Awake()
        {
            I = this;
            Best = PlayerPrefs.GetInt(BestKey, 0);
        }

        void Start()
        {
            EnterMenu();
        }

        void Update()
        {
            InputHelper.Poll();
            stateTime += Time.unscaledDeltaTime;

            switch (State)
            {
                case GameState.Menu:
                    Speed = Mathf.MoveTowards(Speed, 4.5f, 20f * Time.deltaTime);
                    break;

                case GameState.Playing:
                    float ramp = 1f - Mathf.Exp(-Distance / 1800f);
                    float target = Mathf.Lerp(BaseSpeed, MaxSpeed, ramp);
                    if (Player != null && Player.BoostT > 0f) target *= 1.6f;
                    bool dead = Player != null && Player.Dead;
                    if (dead) target = 0f;
                    Speed = Mathf.MoveTowards(Speed, target, (dead ? 40f : 14f) * Time.deltaTime);
                    if (!dead) Distance += Speed * Time.deltaTime;
                    if (InputHelper.Pause && !dead) Pause();
                    break;

                case GameState.Paused:
                    if (InputHelper.Pause) Resume();
                    break;

                case GameState.GameOver:
                    Speed = Mathf.MoveTowards(Speed, 0f, 30f * Time.deltaTime);
                    break;
            }
        }

        void SetState(GameState s)
        {
            State = s;
            stateTime = 0f;
            Time.timeScale = s == GameState.Paused ? 0f : 1f;
            StateChanged?.Invoke(s);
        }

        // ---- public API used by UI and other systems -------------------------------------

        public void EnterMenu()
        {
            Flowers = 0;
            Distance = 0;
            NewBest = false;
            if (World != null) World.ResetWorld();
            if (Player != null) Player.ResetPlayer();
            SetState(GameState.Menu);
        }

        public void StartRun()
        {
            Flowers = 0;
            Distance = 0;
            NewBest = false;
            Speed = BaseSpeed * 0.6f;
            if (World != null) World.ResetWorld();
            if (Player != null) Player.ResetPlayer();
            SetState(GameState.Playing);
        }

        public void Pause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void AddFlower(int n = 1)
        {
            Flowers += n;
            FlowerCollected?.Invoke(Flowers);
        }

        public void PlayerDied()
        {
            if (State != GameState.Playing) return;
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            // keep the state as Playing for a moment so the stumble animation and camera shake play out
            yield return new WaitForSeconds(0.9f);
            if (State != GameState.Playing) yield break;
            if (Score > Best)
            {
                Best = Score;
                NewBest = true;
                PlayerPrefs.SetInt(BestKey, Best);
                PlayerPrefs.Save();
            }
            SetState(GameState.GameOver);
        }
    }
}
