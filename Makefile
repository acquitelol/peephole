VENDOR = src/vendor
RAYLIB_INCLUDE = ../../raylib-5.5_macos/include

default: dldhgh

run: dldhgh
	./$<
	
$(VENDOR)/scale_uv.o: $(VENDOR)/scale_uv.c
	cc $< -o $@ -I$(RAYLIB_INCLUDE) -c

dldhgh: src/main.le $(VENDOR)/scale_uv.o
	ellec $< $(VENDOR)/scale_uv.o -z -lraylib -z -Wl,-rpath,$(HOME)/.local/lib -o $@ --nogc 