#!/bin/bash

session=arr-svc

tmux new -s $session -d

tmux split-window -h -t $session
tmux resize-pane -t $session -x 5

tmux split-window -v -t $session
tmux send-keys -t $session 'cd src' C-m
tmux send-keys -t $session 'dotnet watch run' C-m

tmux select-pane -L -t $session
tmux send-keys -t $session 'nvim' C-m

tmux attach -t $session
