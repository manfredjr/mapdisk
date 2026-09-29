"""Gera o mapdisk.ico em DIB (bitmap sem compressao) a partir de um .ico com PNG dentro.

Uso: python ferramentas/gerar-icone.py <origem.ico> src/mapdisk/recursos/mapdisk.ico

Cada tamanho vira BITMAPINFOHEADER de 32 bits com mascara AND, o formato classico do
Windows. O MapNet usa DIB porque o icone so com PNG trouxe bloqueio no CronoAula.
Precisa do Pillow.
"""

import io
import struct
import sys

from PIL import Image


def ler_tamanhos(caminho):
    dados = open(caminho, "rb").read()
    quantos = struct.unpack("<H", dados[4:6])[0]
    imagens = []
    for i in range(quantos):
        _, _, _, _, _, _, tamanho, inicio = struct.unpack("<BBBBHHII", dados[6 + 16 * i:22 + 16 * i])
        bloco = dados[inicio:inicio + tamanho]
        imagens.append(Image.open(io.BytesIO(bloco)).convert("RGBA"))
    return sorted(imagens, key=lambda im: im.width)


def dib(imagem):
    largura, altura = imagem.size
    cabecalho = struct.pack("<IiiHHIIiiII", 40, largura, altura * 2, 1, 32, 0, 0, 0, 0, 0, 0)
    pixels = bytearray()
    for y in range(altura - 1, -1, -1):
        for x in range(largura):
            r, g, b, a = imagem.getpixel((x, y))
            pixels += bytes((b, g, r, a))
    linha_mascara = ((largura + 31) // 32) * 4
    mascara = bytearray()
    for y in range(altura - 1, -1, -1):
        linha = bytearray(linha_mascara)
        for x in range(largura):
            if imagem.getpixel((x, y))[3] == 0:
                linha[x // 8] |= 0x80 >> (x % 8)
        mascara += linha
    return cabecalho + bytes(pixels) + bytes(mascara)


def gravar(imagens, destino):
    blocos = [dib(im) for im in imagens]
    saida = bytearray(struct.pack("<HHH", 0, 1, len(blocos)))
    inicio = 6 + 16 * len(blocos)
    for im, bloco in zip(imagens, blocos):
        lado = 0 if im.width >= 256 else im.width
        saida += struct.pack("<BBBBHHII", lado, lado, 0, 0, 1, 32, len(bloco), inicio)
        inicio += len(bloco)
    for bloco in blocos:
        saida += bloco
    open(destino, "wb").write(saida)


if __name__ == "__main__":
    origem, destino = sys.argv[1], sys.argv[2]
    imagens = ler_tamanhos(origem)
    gravar(imagens, destino)
    print("gravado:", destino, [im.width for im in imagens])
