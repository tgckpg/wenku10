#!/usr/bin/env python3
import re
import socket
import collections

import sys
import codecs
import datetime

# Symbolic name meaning all available interfaces
HOST = ""
# Arbitrary non-privileged port
PORT = 9730

# UDP: SOCK_DGRAM
s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
s.bind((HOST, PORT))

filter = None;

if 1 < len( sys.argv ) != None:
	filter = sys.argv[1:]

def logHeat(_type):
	# normal
	color = '\x1b[0m';

	# Green
	if ( _type < 10 ): color = '\x1b[32m'
	# normal
	elif ( _type < 20 ): color = '\x1b[0m'
	# yellow
	elif ( _type < 30 ): color = '\x1b[33m'
	# red
	elif ( _type < 40 ): color = '\x1b[31m'
	# MISC
	elif ( _type < 60 ): color = '\x1b[0m'
	# white
	elif ( _type < 60 ): color = '\x1b[1m'

	types = collections.defaultdict( lambda: 'UNK' )
	types[0]  = 'SYSTEM'
	types[10] = 'INFO'
	types[11] = 'DEBUG'
	types[20] = 'WARN'
	types[30] = 'ERROR'
	types[50] = 'TestStart'
	types[51] = 'TestEnd'
	types[52] = 'TestResult'

	return color + types[_type] + '\x1b[0m'


status = re.compile('^(\d+) ([^ ]+)* ?([\w\W]*)')

print( 'Listening on: ' + str(PORT) )
while 1:
	data, addr = s.recvfrom( 65536 )
	mesg = status.match(data.strip(codecs.BOM_UTF8).decode( "utf-8" ))
	id = mesg.group(2)

	if mesg:
		canPrint = False if filter != None and id not in filter else True

		if canPrint:

			print(
				'[{3}][{0}][{1}] {2}'.format(
					logHeat( int( mesg.group(1) ) )
					, id
					, mesg.group(3)
					, datetime.date.strftime(
						datetime.datetime.now(), "%H:%M:%S.%f"
					)
				)
			)
