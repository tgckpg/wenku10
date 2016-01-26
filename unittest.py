#!/usr/bin/env python3
import re
import socket
import collections

import sys

# Symbolic name meaning all available interfaces
HOST = ""
# Arbitrary non-privileged port
PORT = 9730

# TCP: SOCK_STREAM
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.bind((HOST, PORT))
s.listen(1)
print( "Listening on: " + str(PORT) )

conn, addr = s.accept()
print( "Connected by: ", addr )

while 1:
	data = conn.recv(2048)
	if( data.decode( "utf-8" ) == "CanITest" ):
		print( "Received UnitTestSignal" )
		conn.sendall( b"GoAhead" )
		break
	elif( data ):
		print( "Unknow signal:", data )
		conn.sendall( b"No" )
		break
s.close()
