CMAKE_MINIMUM_REQUIRED(VERSION 3.0)

SET (MAIN_IFACES sources/VTK2UnityConverter.h)
SET (MAIN_SRCS sources/VTK2UnityConverter.cxx)

SET (TEST_IFACES tests/VTK2UnityConverterTest.h)
SET (TEST_SRCS tests/VTK2UnityConverterTest.cxx)

SET (EXTRA_LIBS vtkRenderingFreeType vtkIOExport)

INCLUDE_DIRECTORIES (sources)

IF (NOT ParaView_BINARY_DIR)
  FIND_PACKAGE (ParaView REQUIRED)
  INCLUDE (${PARAVIEW_USE_FILE})
ENDIF ()

IF(PARAVIEW_BUILD_QT_GUI)
  # We need to wrap for Qt stuff such as signals/slots etc. to work correctly.
  IF (PARAVIEW_QT_VERSION VERSION_GREATER "4")
  	 QT5_WRAP_CPP(MOC_PLUGIN_SRCS sources/Unity3D.h)
     QT5_WRAP_CPP(MOC_SRCS ${MAIN_IFACES})
     QT5_WRAP_CPP(MOC_TEST_SRCS ${TEST_IFACES})
  ELSE ()
     QT5_WRAP_CPP(MOC_PLUGIN_SRCS sources/Unity3D.h)
     QT4_WRAP_CPP(MOC_SRCS sources/Unity3D.h ${MAIN_IFACES})
     QT4_WRAP_CPP(MOC_TEST_SRCS ${TEST_IFACES})
  ENDIF ()

  # This is a macro for adding QActionGroup subclasses automatically as toolbars.
  ADD_PARAVIEW_ACTION_GROUP (IFACES IFACE_SRCS
                            CLASS_NAME Unity3D
                            GROUP_NAME "ToolBar/Unity3D")

  # Now crete a plugin for the toolbar. Here we pass IFACES and IFACE_SRCS
  # which are filled up by the above macro with relevant entries
  ADD_PARAVIEW_PLUGIN (Unity3D "1.0"
                      GUI_INTERFACES ${IFACES}
                      GUI_RESOURCES resources/Unity3D.qrc
                      SOURCES sources/Unity3D.cxx ${MAIN_SRCS} 
                      ${MOC_SRCS} ${MOC_PLUGIN_SRCS} 
                      ${IFACE_SRCS})
                      
  ADD_EXECUTABLE (Unity3DTests ${MAIN_SRCS} ${MOC_SRCS} 
  	             ${TEST_SRCS} ${MOC_TEST_SRCS})
                      
  TARGET_LINK_LIBRARIES (Unity3DTests ${EXTRA_LIBS}
                        ${QT_QTTEST_LIBRARY} ${QT_LIBRARIES})

  TARGET_LINK_LIBRARIES (Unity3D LINK_PRIVATE ${EXTRA_LIBS})

ENDIF ()

ADD_TEST (NAME Unity3DTests COMMAND Unity3DTests)