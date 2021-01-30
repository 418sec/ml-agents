#!/bin/bash
for file in $(find . -type f); 
do  
  git blame --date=format:%Y%m%d $file
done | sed -e 's/.*\s\([0-9]\{8\}\)\s.*/\1/' | sort -r | tail
