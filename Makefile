default: peephole

run: peephole
	./$<

peephole: src/main.le
	ellec $< -z -lraylib -z -Wl,-rpath,$(HOME)/.local/lib -o $@ --cpfmt