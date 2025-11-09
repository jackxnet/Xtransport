Imports System
Imports System.Text

Public Class FragFrame

    'Private Aframe = New List(Of Byte())
    Private alock As Object
    Private currentframe As Integer = 1

    Public Function buildsegmentheader(segmentinfo As Char())
        Dim byte1 = BitConverter.GetBytes(segmentinfo(0))
        Dim byte2 = BitConverter.GetBytes(segmentinfo(1))
        Dim segmentheader(7) As Byte

        Array.Copy(byte1, segmentheader, 4)
        Array.Copy(byte2, 0, segmentheader, 4, 4)

        Return segmentheader
    End Function


    'routingcode = routeinfo(0)
    'toareacode = routeinfo(1)
    'tolocalcode = routeinfo(2)
    'channelid = routeinfo(3)
    'fromareacode = routeinfo(4)
    'fromlocalid = routeinfo(5)
    Public Function buildrouteheader(routeinfo As Char())

        Dim routeheader(23) As Byte

        Dim byte1 = BitConverter.GetBytes(routeinfo(0))
        Dim byte2 = BitConverter.GetBytes(routeinfo(1))
        Dim byte3 = BitConverter.GetBytes(routeinfo(2))
        Dim byte4 = BitConverter.GetBytes(routeinfo(3))
        Dim byte5 = BitConverter.GetBytes(routeinfo(4))
        Dim byte6 = BitConverter.GetBytes(routeinfo(5))

        Array.Copy(byte1, routeheader, 4)
        Array.Copy(byte2, 0, routeheader, 4, 4)
        Array.Copy(byte3, 0, routeheader, 8, 4)
        Array.Copy(byte4, 0, routeheader, 12, 4)
        Array.Copy(byte5, 0, routeheader, 16, 4)
        Array.Copy(byte6, 0, routeheader, 20, 4)


        Return routeheader
    End Function

    Public Function buildpayloadheader(type As Char, code As Char, nickname As String, info As String)
        Dim byte1 = BitConverter.GetBytes(type)
        Dim byte2 = BitConverter.GetBytes(code)
        Dim string1 = Encoding.UTF8.GetBytes(nickname)
        Dim string2 = Encoding.UTF8.GetBytes(info)

        Dim payload(169) As Byte

        Array.Copy(byte1, payload, 4)
        Array.Copy(byte2, 0, payload, 4, 4)
        Array.Copy(string1, 0, payload, 8, string1.Length)
        Array.Copy(string2, 0, payload, 70, string2.Length)


        Return payload

    End Function

    Public Function frag(b As Byte()) As List(Of Byte()) ' header is 70 bytes
        Dim aframe = New List(Of Byte())
        Dim sequence = 0
        currentframe += 1
        'If currentframe = 10000001 Then currentframe = 0

        Dim aa = 0
        Dim newframe(mtu + 19) As Byte
        'Console.WriteLine(mtu)

        'this is frame header on every frame
        Array.Copy(BitConverter.GetBytes(sequence), 0, newframe, 4, 4)
        ' don't add total frags here that is calculated on Aframe.count
        'Array.Copy(BitConverter.GetBytes(Now().ToOADate), 0, newframe, 12, 8)
        Array.Copy(BitConverter.GetBytes(b.Length), 0, newframe, 12, 4)


        Do While aa < b.Length - mtu

            Array.Copy(b, aa, newframe, 20, mtu)
            aa += mtu
            Dim addit(newframe.Length - 1) As Byte
            Array.Copy(newframe, addit, newframe.Length)
            aframe.Add(addit)
            sequence += 1
            ' fragcount += 1

        Loop
        Dim newsize = b.Length - aa
        ReDim newframe(newsize + 19)
        'Array.Copy(BitConverter.GetBytes(Now().ToOADate), 0, newframe, 12, 8)
        Array.Copy(b, aa, newframe, 20, newsize)
        aframe.Add(newframe)
        'Console.WriteLine($"Assembling frame {currentframe}")

        ' add total frags here at end now that we have a count
        Dim fragcount As Integer = aframe.Count
        For t = 0 To aframe.Count - 1
            Array.Copy(BitConverter.GetBytes(currentframe), 0, aframe(t), 0, 4)
            Array.Copy(BitConverter.GetBytes(t), 0, aframe(t), 4, 4)
            Array.Copy(BitConverter.GetBytes(fragcount), 0, aframe(t), 8, 4)
            Array.Copy(BitConverter.GetBytes(b.Length), 0, aframe(t), 12, 4)

        Next

        Return aframe
    End Function






End Class
