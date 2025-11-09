# xTransporter

This is a transport protocol that uses UDP PXP message type packets that
can by route with guarantee replacing TCP/IP.
Host to client communication, send by channel Unicast or Multicast messages.

I have built alarm systems, security camera, voice, video, chat messaging
database communication, file backup to multiple raspberry Pi's using
multiple connections to each pi. Easy to add a Lora Bridge for routing messages
through the internet, Sip bridges. Replaces TCP/IP with an alway on connection
to the host.

Simple to start hosting using the SetSocket Class
Simple to start Client by using nodeclient class

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
  xcf.broadcast = 0 ' set broadcast on off to for hello packet
  xcf.hostport = port
  fsclient.config(xcf)
  fsclient.setmtu(1500)
  fsclient.startclient("192.168.0.215")