using System.Collections;
using UnityEngine;

namespace _SchoolFestival_Voxel.Scene.Demo.Hitstopscript
{
    public class HitStopManager : MonoBehaviour
    {
        // �V���O���g���p�^�[���łǂ�����ł��A�N�Z�X�ł���悤�ɂ���
        public static HitStopManager Instance { get; private set; }

        // ����TimeScale��ۑ����Ă������߂̕ϐ� (�O�̂���)
        private float originalTimeScale;

        private void Awake()
        {
            // �V���O���g���̐ݒ�
            if (Instance == null)
            {
                Instance = this;
                // �V�[�����܂����ł��j������Ȃ��悤�ɂ���ꍇ�́A�ȉ����R�����g����
                // DontDestroyOnLoad(gameObject); 
            }
            else
            {
                Destroy(gameObject);
            }

            // ����TimeScale��ۑ�
            originalTimeScale = Time.timeScale;
        }

        /// <summary>
        /// �w�肵�����Ԃ����q�b�g�X�g�b�v�i���Ԓ�~�j�𔭐������܂��B
        /// Time.timeScale��0�ɐݒ肵�A�w�莞�Ԍ�Ɍ��ɖ߂��܂��B
        /// </summary>
        /// <param name="duration">�q�b�g�X�g�b�v�̎������ԁi�����ԁA�b�j</param>
        public static void HitStop(float duration)
        {
            if (Instance != null)
            {
                // �R���[�`���Ŏ��Ԑ�����s��
                Instance.StartCoroutine(Instance.DoHitStop(duration));
            }
            else
            {
                Debug.LogError("HitStopManager�̃C���X�^���X���V�[���Ɍ�����܂���B");
            }
        }

        // �q�b�g�X�g�b�v����������R���[�`��
        private IEnumerator DoHitStop(float duration)
        {
            // Time.timeScale������0�̏ꍇ�́A���d���s������邽�ߏ������X�L�b�v
            if (Time.timeScale == 0f)
            {
                yield break;
            }

            // 1. TimeScale�𑀍삵�ăq�b�g�X�g�b�v���Č�����
            Time.timeScale = 0f;

            // 2. ���\�b�h���Ăяo���ăq�b�g�X�g�b�v��������悤�ɂ���
            // TimeScale��0�̊Ԃł��J�E���g�����WaitForSecondsRealtime���g�p
            yield return new WaitForSecondsRealtime(duration);

            // TimeScale�����ɖ߂�
            Time.timeScale = originalTimeScale;
        }

        private void OnDestroy()
        {
            // �X�N���v�g���j�������Ƃ���TimeScale�����Z�b�g
            if (Instance == this)
            {
                Time.timeScale = originalTimeScale;
                Instance = null;
            }
        }
    }
}