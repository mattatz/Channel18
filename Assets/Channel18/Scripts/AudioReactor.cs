using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ
{

    public abstract class AudioReactor : MonoBehaviour, IAudioReactable, IOSCReactable {

        public string Address { get { return audioAddress; } }
        public bool Reactive { get { return reactive; } }

        [SerializeField] protected string audioAddress = "/effect/audio";
        [SerializeField] protected bool reactive;

        public void OnReact(int index, bool on)
        {
            if (!reactive) return;
            React(index, on);
        }

        protected abstract void React(int index, bool on);

        public virtual void OnOSC(string addr, List<object> data)
        {
            if (addr != audioAddress) return;

            if(data.Count > 0) {
                int flag;
                var tmp = reactive;
                var next = tmp;
                if(int.TryParse(data[0].ToString(), out flag)) {
                    next = (flag == 1);
                }
                if(tmp != next && !next) {
                    // if off to on
                    for (int i = 0; i < 8; i++)
                    {
                        OnReact(i, false);
                    }
                }
                reactive = next;
            }
        }

    }

}


