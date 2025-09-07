using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using UniRx;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _audioClip;
    [Inject] private IGameProgressIndicatable _progressIndicatable;
    [Inject] private IRecipeObservable _recipeObservable;

    public void Start()
    {
        _progressIndicatable.OnStartGame
            .Subscribe(_ => _audioSource.Play()).AddTo(this);

        _recipeObservable.RecipeName
             .Subscribe(_ => OnPlaySound());
    }

    public void OnPlaySound()
    {
        _audioSource.PlayOneShot(_audioClip);
    }

}
