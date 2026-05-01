# Usage

```
OPTIONS:
  -b, --buyer <buyer>
  -r, --reference <reference>
  -p, --payment <payment>
  -d, --description <description>
  -f, --fee <fee>
```

## Commissions

``cdb --buyer <buyer> --reference <reference> --payment <payment> --description <description> --fee <fee>``

Example: ``cdb -b "Alberta Mann" -r "https://www.furaffinity.net/view/20700210/" -f 43 -p "5458-2118-9194-8514" -d "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vestibulum ut pretium enim. Sed a neque."``

```csv
Buyer,Reference,Payment,Description,Fee
"Alberta Mann","https://www.furaffinity.net/view/20700210/","alberta@example.com","Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vestibulum ut pretium enim. Sed a neque.","43"
```

## YCH

``cdb ych --b <buyer> -f <fee> -r <reference> -d <description> -p <payment> -y <ych> -s <slot>``

Example: ``cdb ych -b "Bessie Hettinger" -s 4 -f 25 -p "Bessie@example.com" -r "https://www.furaffinity.net/view/20700210/" -y Synthesize -c SEK``

```csv
Date,Client,Reference,Fee,Payment,YCH,Slot
10/26/2020,Bessie Hettinger,https://www.furaffinity.net/view/20700210/,"25,00kr",Bessie@example.com,Synthesize,4
```

## Raffle

TBA. 

See issue [#1](https://github.com/tonytins/artm/issues/1).