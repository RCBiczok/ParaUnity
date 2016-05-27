CMAKE_MINIMUM_REQUIRED(VERSION 3.0)

IF(NOT ParaView_BINARY_DIR)
  FIND_PACKAGE(ParaView REQUIRED)
  INCLUDE(${PARAVIEW_USE_FILE})
ENDIF()

IF(PARAVIEW_BUILD_QT_GUI)
  # We need to wrap for Qt stuff such as signals/slots etc. to work correctly.
  IF (PARAVIEW_QT_VERSION VERSION_GREATER "4")
    QT5_WRAP_CPP(MOC_SRCS Unity3D.h LoadingSplashScreen.h)
  ELSE ()
    QT4_WRAP_CPP(MOC_SRCS Unity3D.h LoadingSplashScreen.h)
  ENDIF ()

  SET(PLATFORM_QRC "${CMAKE_CURRENT_BINARY_DIR}/platform.qrc")

  SET(PLAYER_BUILD_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../../Unity/ParaViewEmbeddedPlayer/build")

  FILE(REMOVE ${PLATFORM_QRC})

  IF(WIN32)
     MESSAGE(FATAL_ERROR "Unsupported architecture" )
  ELSEIF(APPLE)
    SET(BUNDLE_NAME unity_player.app)
    SET(BUNDLE_PATH "${PLAYER_BUILD_DIR}/${BUNDLE_NAME}")

    IF(NOT EXISTS ${BUNDLE_PATH})
      MESSAGE(FATAL_ERROR "Unity player binary ${BUNDLE_PATH} does not exist. You have to create it first by using the Unity Editor" )
    ENDIF()

    FILE(REMOVE_RECURSE ${CMAKE_CURRENT_BINARY_DIR}/${BUNDLE_NAME})

    FILE(COPY ${BUNDLE_PATH} DESTINATION ${CMAKE_CURRENT_BINARY_DIR})

    FILE(GLOB_RECURSE BUNDLE_FILES RELATIVE ${CMAKE_CURRENT_BINARY_DIR} "${CMAKE_CURRENT_BINARY_DIR}/${BUNDLE_NAME}/*")

    FILE(APPEND ${PLATFORM_QRC} "<RCC>\n")
    FILE(APPEND ${PLATFORM_QRC} "  <qresource prefix=\"/Unity3D/platform\">\n")

    FOREACH(BUNDLE_FILE ${BUNDLE_FILES})
      FILE(APPEND ${PLATFORM_QRC} "   <file>${BUNDLE_FILE}</file>\n")
    ENDFOREACH()

    FILE(APPEND ${PLATFORM_QRC} "  </qresource>\n")
    FILE(APPEND ${PLATFORM_QRC} "</RCC>")

  ELSE()
     MESSAGE(FATAL_ERROR "Unsupported architecture" )
  ENDIF()

  # This is a macro for adding QActionGroup subclasses automatically as toolbars.
  ADD_PARAVIEW_ACTION_GROUP (IFACES IFACE_SRCS
                            CLASS_NAME Unity3D
                            GROUP_NAME "ToolBar/Unity3D")

  # Now crete a plugin for the toolbar. Here we pass IFACES and IFACE_SRCS
  # which are filled up by the above macro with relevant entries
  ADD_PARAVIEW_PLUGIN(Unity3D "1.0"
                      GUI_INTERFACES ${IFACES}
                      GUI_RESOURCES Unity3D.qrc ${PLATFORM_QRC}
                      SOURCES ${MOC_SRCS} ${IFACE_SRCS}
                      Unity3D.cxx)
ENDIF()

TARGET_LINK_LIBRARIES (Unity3D
  LINK_PRIVATE
#    vtkRenderingFreeType
#    vtkPVAnimation
#    vtkPVServerManagerApplication
#    vtkPVApplicationCore
#    vtkPVServerManagerCore
    pqApplicationComponents
    vtkIOExport
)
