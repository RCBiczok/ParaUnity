#include "VTK2UnityConverterTest.h"
#include <QString.h>

void VTK2UnityConverterTest::toUpper()
{
    QString str = "Hello";
    QVERIFY(str.toUpper() == "HELLO");
}

QTEST_MAIN(VTK2UnityConverterTest)
