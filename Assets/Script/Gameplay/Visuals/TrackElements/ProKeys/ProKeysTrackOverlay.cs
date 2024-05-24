﻿using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using YARG.Core.Game;
using YARG.Core.Logging;
using YARG.Gameplay.Player;
using YARG.Helpers;
using YARG.Helpers.Extensions;

namespace YARG.Gameplay.Visuals
{
    public class ProKeysTrackOverlay : MonoBehaviour
    {
        [SerializeField]
        private GameObject _keyOverlayPrefabBig;
        [SerializeField]
        private GameObject _keyOverlayPrefabSmall;

        [Space]
        [SerializeField]
        private Texture2D _edgeGradientTexture;
        [SerializeField]
        private Texture2D _heldGradientTexture;

        [SerializeField]
        private float _trackWidth = 2f;

        [Space]
        [SerializeField]
        private float _whiteKeyOffset;
        [SerializeField]
        private float _blackKeyOffset;

        public float KeySpacing => _trackWidth / ProKeysPlayer.WHITE_KEY_VISIBLE_COUNT;

        private readonly List<GameObject> _highlights = new();

        private static readonly int IsHighlight = Shader.PropertyToID("_IsHighlight");
        private static readonly int BaseMap     = Shader.PropertyToID("_BaseMap");

        public void Initialize(TrackPlayer player, ColorProfile.ProKeysColors colors)
        {
            int overlayPositionIndex = 0;

            _highlights.Clear();
            int whitePositionIndex = 0;
            int blackPositionIndex = 0;

            for (int i = 0; i < ProKeysPlayer.TOTAL_KEY_COUNT; i++)
            {
                int noteIndex = i % 12;
                int octaveIndex = i / 12;

                // Get the group index (two groups per octave)
                int group = octaveIndex * 2 + (PianoHelper.IsLowerHalfKey(noteIndex) ? 0 : 1);
                var groupColor = colors.GetOverlayColor(group).ToUnityColor();

                if (PianoHelper.IsBlackKey(noteIndex))
                {
                    SpawnHighlight(true, blackPositionIndex, player, groupColor);
                    blackPositionIndex++;

                    if (PianoHelper.IsGapOnNextBlackKey(noteIndex))
                    {
                        blackPositionIndex++;
                    }
                }
                else
                {
                    SpawnHighlight(false, whitePositionIndex, player, groupColor);
                    whitePositionIndex++;

                    SpawnOverlay(overlayPositionIndex, noteIndex, player, groupColor);
                    overlayPositionIndex++;
                }
            }
        }

        private void SpawnHighlight(bool isBlackKey, int index, TrackPlayer player, Color color)
        {
            var prefab = isBlackKey
                ? _keyOverlayPrefabSmall
                : _keyOverlayPrefabBig;
            var offset = isBlackKey
                ? _blackKeyOffset
                : _whiteKeyOffset;

            var highlight = Instantiate(prefab, transform);
            highlight.transform.localPosition = new Vector3(index * KeySpacing + offset, 0f, 0f);

            var material = highlight.GetComponentInChildren<MeshRenderer>().material;
            material.color = color.WithAlpha(0.3f);
            material.SetTexture(BaseMap, _heldGradientTexture);
            material.SetFade(player.ZeroFadePosition, player.FadeSize);

            highlight.SetActive(false);
            _highlights.Add(highlight);
        }

        private void SpawnOverlay(int index, int noteIndex, TrackPlayer player, Color color)
        {
            // Spawn overlay

            var overlay = Instantiate(_keyOverlayPrefabBig, transform);
            overlay.transform.localPosition = new Vector3(index * KeySpacing + _whiteKeyOffset, 0f, 0f);

            var material = overlay.GetComponentInChildren<MeshRenderer>().material;
            material.color = color.WithAlpha(0.05f);
            material.SetFade(player.ZeroFadePosition, player.FadeSize);

            // Set up the correct texture

            var (edge, flip) = noteIndex switch
            {
                0 or 5  => (true, false),
                4 or 11 => (true, true),
                _       => (false, false)
            };

            if (edge)
            {
                material.SetTexture(BaseMap, _edgeGradientTexture);
            }

            if (flip)
            {
                material.SetTextureScale(BaseMap, new Vector2(-1f, 1f));
                material.SetTextureOffset(BaseMap, new Vector2(1f, 0f));
            }
        }

        public void SetKeyHeld(int keyIndex, bool held)
        {
            if (keyIndex < 0 || keyIndex >= _highlights.Count) return;

            YargLogger.LogFormatDebug("Setting key held: {0}", keyIndex);

            _highlights[keyIndex].SetActive(held);
        }
    }
}