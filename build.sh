#!/bin/bash

currentdir=$(pwd) 			# save current path
cd src/Pulse				# go into pulse
dotnet clean -c Release		# clear old binaries
dotnet build -c Release		# build
cd $currentdir				# return to source path