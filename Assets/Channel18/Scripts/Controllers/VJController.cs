using System;
using System.Net;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using MidiJack;
using UnityOSC;

namespace VJ.Channel18
{

    [System.Serializable]
    public class OSCEvent : UnityEvent<string, List<object>> {}

    [System.Serializable]
    public class MidiNoteEvent : UnityEvent<int> {}

    [System.Serializable]
    public class MidiKnobEvent : UnityEvent<int, float> {}

    [System.Serializable]
    public class AudioEvent : UnityEvent<int, bool> {}

    public class VJController : MonoBehaviour {

        [SerializeField] protected int port = 8888;
        [SerializeField] protected OSCEvent onOsc;
        [SerializeField] protected MidiNoteEvent onNoteOn, onNoteOff;
        [SerializeField] protected MidiKnobEvent onKnob;
        [SerializeField] protected AudioEvent onAudio;

        [SerializeField] protected List<AudioReaction> reactions;

        #region OSC variables

        OSCServer server;
        protected List<OSCPacket> packets;

        #endregion

        void Start () {
            server = CreateServer("Channel18", port);
            packets = new List<OSCPacket>();

            foreach(var go in GameObject.FindObjectsOfType<GameObject>())
            {
                var kontrollables = go.GetComponents<INanoKontrollable>();
                if(kontrollables.Length > 0) {
                    foreach(var kontrollable in kontrollables)
                    {
                        onNoteOn.AddListener(kontrollable.NoteOn);
                        onNoteOff.AddListener(kontrollable.NoteOff);
                        onKnob.AddListener(kontrollable.Knob);
                    }
                }

                var oscReactables = go.GetComponents<IOSCReactable>();
                if(oscReactables.Length > 0) {
                    foreach(var oscReactable in oscReactables)
                    {
                        onOsc.AddListener(oscReactable.OnOSC);
                    }
                }

                var audioReactables = go.GetComponents<IAudioReactable>();
                if(audioReactables.Length > 0) {
                    foreach(var audioReactable in audioReactables) {
                        onAudio.AddListener(audioReactable.OnReact);
                    }
                }
            }
        }
        
        void Update () {
            for(int i = 0, n = packets.Count; i < n; i++)
            {
                var packet = packets[i];
                onOsc.Invoke(packet.Address, packet.Data);

                switch(packet.Address)
                {
                    case "/audio/fft/8":
                        var spectrums = packet.Data.Select(d => float.Parse(d.ToString())).ToList();
                        React(spectrums);
                        break;

                    case "/audio/fft/peaks":
                        var peaks = packet.Data.Select(d => float.Parse(d.ToString())).ToList();
                        Peak(peaks);
                        break;
                }
            }
            packets.Clear();
        }

        void OnEnable()
        {
            MidiMaster.noteOnDelegate += NoteOn;
            MidiMaster.noteOffDelegate += NoteOff;
            MidiMaster.knobDelegate += Knob;
        }

        void OnDisable()
        {
            MidiMaster.noteOnDelegate -= NoteOn;
            MidiMaster.noteOffDelegate -= NoteOff;
            MidiMaster.knobDelegate -= Knob;
        }

        void OnApplicationQuit() 
        {
            if(server != null)
            {
                server.Close();
            }
        }

        #region Midi functions

        void NoteOn(MidiChannel channel, int note, float velocity)
        {
            Debug.Log("NoteOn: " + channel + "," + note + "," + velocity);
            onNoteOn.Invoke(note);
        }

        void NoteOff(MidiChannel channel, int note)
        {
            // Debug.Log("NoteOff: " + channel + "," + note);
            onNoteOff.Invoke(note);
        }

        void Knob(MidiChannel channel, int knobNumber, float knobValue)
        {
            Debug.Log("Knob: " + knobNumber + "," + knobValue);
            onKnob.Invoke(knobNumber, knobValue);
        }

        #endregion

        #region OSC functions

        OSCServer CreateServer(string serverId, int port)
        {
            OSCServer server = new OSCServer(port);
            server.PacketReceivedEvent += OnPacketReceived;

            ServerLog serveritem = new ServerLog();
            serveritem.server = server;
            serveritem.log = new List<string>();
            serveritem.packets = new List<OSCPacket>();

            return server;
        }

        void OnPacketReceived(OSCServer server, OSCPacket packet)
        {

            packets.Add(packet);
        }
               
        #endregion

        protected void React(List<float> spectrums)
        {
            int n = spectrums.Count, m = reactions.Count;
            for(int i = 0; i < n && i < m; i++)
            {
                var s = spectrums[i];
                var r = reactions[i];
                if(r.Trigger(s)) {
                    onAudio.Invoke(i, r.On);
                }
            }
        }

        protected void Peak(List<float> peaks)
        {
            int n = peaks.Count, m = reactions.Count;
            for(int i = 0; i < n && i < m; i++)
            {
                var p = peaks[i];
                var r = reactions[i];
                r.Peak = p;
                // force off
                if(r.Trigger(0f)) {
                    onAudio.Invoke(i, r.On);
                }
            }
        }

    }

}


