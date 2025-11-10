# xTransporter

Pxp Communication Protocol allowing multinode to host connections
which are always connected with port/unicast/multicast/port node
to node rounting.

watch 5 pi's with 10 xtransport connections for 4Gb/s Backup
https://github.com/user-attachments/assets/3557bab4-af74-4fea-95f5-cc86295d110e

# KEY Features:

File Hosting
Database Hosting
Routing
Alarm System
Security Camera 
distributed file system
Lora bridge to xPXP network
Sip to xtransport
Voice and Video transport
Multiple Transport (Bonding) for higher throughput
Alway on connection with node retry connection if host lost

# KEY Usages:

host video/voice/podcast to multinodes

cross connect  your(host on multinode) to my(host to multinoce)
to create a cross connect for zoom/ group video/audio hosting
type connections

Use host for routing node to nodes for alarm notify/ messaging/chat
using multichannels for different chat/services

Great for Lora radio bridge to route Lora Messages to internet nodes
or use to route from lora to lora network over the internet. I've done a bridge
plugin using the RYLR998 UART and using different xtransport channels to match
the lora channel set in the 998 UART.

USe host to nodes for server/client type service including
File/database services

use multi (node) connections app to multi ipcams to watch unlimited ipcams
(use the rtp/xtransport bridge on a iot/raspberry pi) for each stream. Record
rtp traffic on a pi and stream playback from pi using xtransport. xtransport
custom messages an be used to request playback from pi. (using the record
/playback plugin)

possible building onion type udp node-host-host-host-node route to
for anonymous ip traffic.

This is a transport protocol that uses UDP message type packets that
can by route with guarantee replacing TCP/IP.

Host to client communication, send by channel Unicast or Multicast messages.

I have built alarm systems, security camera, voice, video, chat messaging

database communication, file backup to multiple raspberry Pi's using

multiple connections to each pi. Easy to add a Lora Bridge for routing messages

through the internet, Sip bridges. Replaces TCP/IP with an alway on connection

to the host.

# Setup Notes:
Easy to build a Host-Service-Plugin for custom host/route services

Simple to start hosting using the SetSocket Class

Simple to start Client by using nodeclient class

Note all traffic is queued to a global concurrent.Queue
regardless of how many xtransports is opened to keep
threadsafe opperations. No calling events

Node queues come off of data_read_node queue
Host queues come off data_ready_host queue

Queues managed with a task.run function vs event handler 


# Easy Host Setup example:

 serversock = new setsocket

 serversock.setspeed(netlatency, packetburst)

 serversock.setportrange(portmin, portmax)

 serversock.InitializeReceiver(ip, port)

 serversock.setmtu(mtu)


 start a task to cleanup dead connections

        Task.Run(Sub() dokilldead())


 Private Sub dokilldead()

        While canceltoken = False

            serversock.checkfordead()

            Sleep(8000)

        End While

    End Sub



# Easy Node Setup Example:


  Public xcf As New XClient

  Public fsclient As New nodeclient
        
  xcf.channel = 1025

  xcf.packetburst = 500

  xcf.netlatency = 75

  xcf.localid = port + rnd.Next(10000)

  xcf.hostid = 10011

  xcf.broadcast = 0  set broadcast on off to for hello packet

  xcf.hostport = port

  fsclient.config(xcf)

  fsclient.setmtu(1500)

  fsclient.startclient("192.168.0.215")


  Packets can be customized for different custom use only requires the
    routing header

   Dim body As New List(Of Byte)

        body.AddRange(BitConverter.GetBytes(routecode))

        body.AddRange(BitConverter.GetBytes(toarea))

        body.AddRange(BitConverter.GetBytes(tolocal))

        body.AddRange(BitConverter.GetBytes(channel))

        body.AddRange(BitConverter.GetBytes(fromarea))

        body.AddRange(BitConverter.GetBytes(fromid))

        body.AddRange(BitConverter.GetBytes(netcode))

        body.AddRange(BitConverter.GetBytes(controlcode))


Will convert to C# when/if necessary

I will be posting a core host example with plugin services



Route by Unicast, channel, Multicast, and by Port

