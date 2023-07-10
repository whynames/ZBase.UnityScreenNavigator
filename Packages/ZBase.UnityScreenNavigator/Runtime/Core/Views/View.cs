using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using ZBase.UnityScreenNavigator.Foundation;

namespace ZBase.UnityScreenNavigator.Core.Views
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class View : UIBehaviour, IView
    {
        [FormerlySerializedAs("_dontUseCanvasGroup")]
        [SerializeField] private bool _dontAddCanvasGroupAutomatically = false;
        [SerializeField] private bool _usePrefabNameAsIdentifier = true;

        [SerializeField]
        [EnabledIf(nameof(_usePrefabNameAsIdentifier), false)]
        private string _identifier;

        public string Identifier
        {
            get => _identifier;
            set => _identifier = value;
        }

        public virtual string Name
        {
            get
            {
                return !IsDestroyed() && gameObject == true ? gameObject.name : string.Empty;
            }

            set
            {
                if (IsDestroyed() || gameObject == false)
                    return;

                gameObject.name = value;
            }
        }

        [SerializeField, HideInInspector]
        private RectTransform _rectTransform;

        public virtual RectTransform RectTransform
        {
            get
            {
                if (IsDestroyed())
                    return null;

                if (_rectTransform == false)
                    _rectTransform = gameObject.GetOrAddComponent<RectTransform>();

                return _rectTransform;
            }
        }

        private RectTransform _parent;

        public virtual RectTransform Parent
        {
            get
            {
                if (IsDestroyed())
                {
                    return null;
                }

                return _parent;
            }

            internal set => _parent = value;
        }
    
        public virtual GameObject Owner
        {
            get { return IsDestroyed() ? null : this.gameObject; }
        }

        public virtual bool ActiveSelf
        {
            get
            {
                GameObject o;
                return IsDestroyed() == false
                    && (o = gameObject) == true
                    && o.activeSelf == true;
            }

            set
            {
                if (IsDestroyed() 
                    || gameObject == false
                    || gameObject.activeSelf == value)
                    return;

                gameObject.SetActive(value);
            }
        }

        public virtual float Alpha
        {
            get
            {
                if (IsDestroyed() || gameObject == false)
                    return 0;

                if (CanvasGroup)
                    return CanvasGroup.alpha;

                return 1f;
            }
            set
            {
                if (IsDestroyed() || gameObject == false)
                    return;

                if (CanvasGroup)
                    CanvasGroup.alpha = value;
            }
        }

        public virtual bool Interactable
        {
            get
            {
                if (IsDestroyed() || gameObject == false)
                    return false;

                if (CanvasGroup)
                    return CanvasGroup.interactable;

                return true;
            }

            set
            {
                if (IsDestroyed() || gameObject == false)
                    return;

                if (CanvasGroup)
                    CanvasGroup.interactable = value;
            }
        }

        [SerializeField, HideInInspector]
        private CanvasGroup _canvasGroup;

        public virtual CanvasGroup CanvasGroup
        {
            get
            {
                if (IsDestroyed())
                    return null;

                if (_canvasGroup == false)
                    _canvasGroup = gameObject.GetComponent<CanvasGroup>();

                if (_canvasGroup == false && _dontAddCanvasGroupAutomatically == false)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();

                return _canvasGroup;
            }
        }

        public bool DontAddCanvasGroupAutomatically => _dontAddCanvasGroupAutomatically;

        public virtual UnityScreenNavigatorSettings Settings { get; set; }

        protected void SetIdentifer()
        {
            _identifier = _usePrefabNameAsIdentifier
                ? gameObject.name.Replace("(Clone)", string.Empty)
                : _identifier;
        }

        protected static async UniTask WaitForAsync(IEnumerable<UniTask> tasks)
        {
            try
            {
                foreach (var task in tasks)
                {
                    await task;
                }
            }
            catch
            {
            }
        }
    }
}