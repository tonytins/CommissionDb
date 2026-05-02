# Usage

```
OPTIONS:
  -c, --client <client>
  -r, --reference <reference>
  -p, --payment <payment>
  -d, --description <description>
  -f, --fee <fee>
```

## Commissions

``cdb --client <client> --reference <reference> --payment <payment> --description <description> --fee <fee>``

Example: ``cdb -c "Alberta Mann" -r "https://www.furaffinity.net/view/64746038/" -f 43 -p "alberta@example.com" -d "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vestibulum ut pretium enim. Sed a neque."``

```csv
Date,Client,Reference,Payment,Description,Fee
"2026-05-01 17:28:07 +0000","Alberta Mann","https://www.furaffinity.net/view/20700210/","5458-2118-9194-8514","Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vestibulum ut pretium enim. Sed a neque.","43"
```

## YCH (Outdated)

``cdb ych --c <client> -f <fee> -r <reference> -d <description> -p <payment> -y <ych> -s <slot>``

Example: ``cdb ych -c "Bessie Hettinger" -s 4 -f 25 -p "bessie@example.com" -r "https://www.furaffinity.net/view/64746038/" -y Synthesize -c SEK``

```csv
Date,Client,Reference,Fee,Payment,YCH,Slot
10/26/2020,Bessie Hettinger,https://www.furaffinity.net/view/20700210/,"25,00kr",Bessie@example.com,Synthesize,4
```

## Raffle

TBA. 

See issue [#1](https://github.com/tonytins/artm/issues/1).