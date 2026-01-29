default: peephole

run: peephole
	$<

peephole: src/main.le
	ellec $< -z -lraylib -o $@ --cpfmt