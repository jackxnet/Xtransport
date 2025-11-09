# xTransporter

watch 5 pi's with 10 xtransport connections for 4Gb/s Backup

https://infinitynull.com/pi/piuploadtest.mkv

This is a transport protocol that uses UDP message type packets that
can by route with guarantee replacing TCP/IP.

Host to client communication, send by channel Unicast or Multicast messages.

I have built alarm systems, security camera, voice, video, chat messaging

database communication, file backup to multiple raspberry Pi's using

multiple connections to each pi. Easy to add a Lora Bridge for routing messages

through the internet, Sip bridges. Replaces TCP/IP with an alway on connection

to the host.


Simple to start hosting using the SetSocket Class

Simple to start Client by using nodeclient class

Note all traffic is queued to a global concurrent.Queue
regardless of how many xtransports is opened to keep
threadsafe opperations. No calling events

Node queues come off of data_read_node queue
Host queues come off data_ready_host queue

Queues managed with a task.run function vs event handler 


Start Hosting Service

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



Starting Node Client example


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

File Hosting
Database Hosting
Routing
Alarm System
Security Camera 
File Backup through distributed Raspberry Pi
File MyPiDrive like onedrive using 
Lora bridge to xPXP network
Sip Bridge
Voice
Video
Multiple Transport and bonding using routing by port number

Alway on connection with node retry connection if host lost
Route by Unicast, channel, Multicast, and by Port

