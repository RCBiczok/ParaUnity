#ifndef LoadingSplashScreen_h
#define LoadingSplashScreen_h

#include <QSplashScreen>
#include <QMovie>
#include <QPainter>
#include <QString>

class LoadingSplashScreen : public QSplashScreen {
  Q_OBJECT
public:
  LoadingSplashScreen(const QString &fileName) : movie(fileName) {
    setPixmap(QPixmap::fromImage(QImage(fileName)));
    movie.start();
    connect(&movie, SIGNAL(updated(QRect)), this, SLOT(frameUpdate()));
  }

  ~LoadingSplashScreen() {}

  void paintEvent(QPaintEvent *event) {
    QPainter painter(this);
    painter.drawPixmap(movie.frameRect(), movie.currentPixmap());
  }

private slots:
  void frameUpdate() {
    setPixmap(movie.currentPixmap());
    update();
  }

private:
  QMovie movie;
};
#endif // LoadingSplashScreen_h