# Content-based recommendation system

Препоръчва продукти, които си приличат по съдържание, не по това кой какво е купил.

Гледа **име, описание, марка, категория и характеристики**. От тях се прави TF-IDF вектор и се мери cosine similarity.

## Папки

| Път | За какво е |
| --- | --- |
| `data/products.csv` | Каталогът за обучение. Смени го с реалните продукти при нужда. |
| `models/` | Тук се записва обученият модел (`content_based.joblib`). |

## Старт

```bash
cd ml/recommendation-system
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python content_based.py train
python content_based.py recommend --item-id 8 -k 4
```

Пример: за `iPhone 18 Pro` (id 8) очаквай близки смартфони/електроника, за крем — други кремове.

## Как да смениш модела

1. Обнови `data/products.csv` (колони: `id,name,description,brand,category,features`).
2. Пусни отново `python content_based.py train`.
3. Новият файл е `models/content_based.joblib`.

По-късно този файл може да се върже към страницата на продукта в сайта.
