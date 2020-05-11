﻿using Entities;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Managers {
    public class GameManager : MonoSingleton<GameManager> {

        #region GameInterface
        [System.Serializable]
        private class GameInterface {

            [SerializeField]
            private List<Image> healthCounts;

            [SerializeField, MustBeAssigned]
            private TextMeshProUGUI scoreText;

            public int TotalHealthCount { get => healthCounts.Count; }

            public void DecreaseHealth() {
                foreach (var health in healthCounts) {
                    if (health.IsActive()) {
                        health.enabled = false;
                        return;
                    }
                }

                Debug.LogWarning("No more health in interface to disable.");
            }

            public void SetScore(int score) {
                scoreText.text = score.ToString();
            }
        }
        #endregion

        [SerializeField]
        private GameInterface gameInterface;

        [SerializeField, Tooltip("How many enemies to kill before upgrade"), PositiveValueOnly]
        private int upgradeIntervals;

        public bool GameOver { get; private set; }

        public int Lives { get; private set; }

        public int Score { get; private set; }

        private int upgradeCounter;

        protected override void OnAwake() {
            Lives = gameInterface.TotalHealthCount;
            Score = 0;
            GameOver = false;

            gameInterface.SetScore(Score);
            upgradeCounter = 0;
        }

        internal void TriggerGameOver() {
            GameOver = true;
            // TODO: Flash game over screen.
        }

        internal void DecreaseHealth() {
            --Lives;
            gameInterface.DecreaseHealth();

            if (Lives <= 0) {
                TriggerGameOver();
            }
        }

        internal void IncreaseScore() { 
            ++Score;
            gameInterface.SetScore(Score);

            ++upgradeCounter;
            if (upgradeCounter >= upgradeIntervals) {
                EnemyManager.Instance.Upgrade();
                Player.Instance.Upgrade();

                // Increase the next upgrade threshold
                upgradeIntervals += Mathf.CeilToInt(upgradeIntervals * 0.001f);

                upgradeCounter = 0;
            }
        }
    }
}
