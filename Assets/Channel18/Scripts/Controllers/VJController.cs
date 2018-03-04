using System;
using System.Net;
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

    public class VJController : MonoBehaviour {

        [SerializeField] protected int port = 8888;
        [SerializeField] protected OSCEvent onOsc;
        [SerializeField] protected MidiNoteEvent onNoteOn, onNoteOff;
        [SerializeField] protected MidiKnobEvent onKnob;

        #region OSC variables

        OSCServer server;
        protected List<OSCPacket> packets;

        #endregion

        void Start () {
            server = CreateServer("Channel18", port);
            packets = new List<OSCPacket>();
        }
        
        void Update () {
            for(int i = 0, n = packets.Count; i < n; i++)
            {
                var packet = packets[i];
                onOsc.Invoke(packet.Address, packet.Data);
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
            // Debug.Log("NoteOn: " + channel + "," + note + "," + velocity);
            onNoteOn.Invoke(note);
        }

        void NoteOff(MidiChannel channel, int note)
        {
            // Debug.Log("NoteOff: " + channel + "," + note);
            onNoteOff.Invoke(note);
        }

        void Knob(MidiChannel channel, int knobNumber, float knobValue)
        {
            // Debug.Log("Knob: " + knobNumber + "," + knobValue);
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

    }

}


